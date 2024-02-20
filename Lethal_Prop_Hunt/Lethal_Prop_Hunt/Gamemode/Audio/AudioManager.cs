using GameNetcodeStuff;
using HarmonyLib;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using LethalPropHunt.Gamemode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx.Configuration;

namespace LethalPropHunt.Audio
{
    //Taken from https://github.com/cmooref17/Lethal-Company-TooManyEmotes/blob/main/TooManyEmotes/Audio/AudioManager.cs
    [HarmonyPatch]
    public static class AudioManager
    {
        public static HashSet<string> propAudioAssetNames = new HashSet<string>();
        public static HashSet<string> hunterAudioAssetNames = new HashSet<string>();


        public static HashSet<AudioClip> loadedAudioClips = new HashSet<AudioClip>();
        public static Dictionary<string, AudioClip> loadedAudioClipsDict = new Dictionary<string, AudioClip>();

        public readonly static string audioFileExtension = ".mp3";

        public static ConfigEntry<float> TauntVolume { get; private set; }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void Init()
        {
            ClearAudioClipCache();
            TauntVolume = PropHuntBase.Instance.Config.Bind("Audio", "Taunt Volume", 0.3f, "The volume you want your taunts to be at, where 1 is 100% volume.");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(TauntVolume, new FloatInputFieldOptions
            {
                Min = 0.00001f,
                Max = 1
            }));
            LoadAudioAssets();
        }


        public static void LoadAudioAssets()
        {
            try
            {
                string[] names = PropHuntBase.bundle.GetAllAssetNames();
                PropHuntBase.mls.LogDebug("Found Assets: " + String.Join(", ", names));
                List<string> propAudio = new List<string>();
                List<string> hunterAudio = new List<string>();
                foreach(string audioFileName in names)
                {
                    if (audioFileName.Contains("assets/lphassets/audio"))
                    {
                        if (audioFileName.Contains("props/"))
                        {
                            propAudio.Add(audioFileName);
                        }
                        else
                        {
                            hunterAudio.Add(audioFileName);
                        }
                    }
                }
                propAudioAssetNames.UnionWith(propAudio.ToArray());
                hunterAudioAssetNames.UnionWith(hunterAudio.ToArray());
                PropHuntBase.mls.LogDebug("Loaded clips Props: " + String.Join(", ", propAudioAssetNames.ToArray<string>()) + " Hunter: " + String.Join(", ", hunterAudioAssetNames.ToArray<string>()));

            }
            catch
            {
                PropHuntBase.mls.LogError("Failed to load emotes audio asset bundle: lphassets.");
            }
        }


        public static bool LoadAllAudioClips()
        {
            if (PropHuntBase.bundle == null)
            {
                PropHuntBase.mls.LogError("Cannot load audio clips with a null Asset Bundle. Did the Asset Bundle fail to load?");
                return false;
            }
            try
            {
                loadedAudioClips.UnionWith(PropHuntBase.bundle.LoadAllAssets<AudioClip>());
                foreach (var clip in loadedAudioClips)
                {
                    if (!loadedAudioClipsDict.ContainsKey(clip.name))
                        loadedAudioClipsDict[clip.name] = clip;
                }
            }
            catch
            {
                PropHuntBase.mls.LogError("Failed to load all emote audio clips from asset bundle.");
                return false;
            }
            return true;
        }
        public static string SelectRandomClip(String type)
        {
            List<string> clips;
            if (type.Equals(LPHRoundManager.PROPS_ROLE))
            {
                clips = propAudioAssetNames.ToList<string>();

            }
            else
            {
                clips = hunterAudioAssetNames.ToList<string>();
            }
            System.Random rand = new System.Random();
            return clips[rand.Next(0, clips.Count)];
        }

        public static AudioClip LoadRandomClip(String type)
        {
            string randomClip = SelectRandomClip(type);
            return LoadAudioClip(randomClip);
        }


        public static AudioClip LoadAudioClip(string clipName)
        {
            if (PropHuntBase.bundle == null)
            {
                PropHuntBase.mls.LogError("Cannot load audio clip: " + clipName + " with a null Asset Bundle. Did the Asset Bundle fail to load?");
                return null;
            }
            if (!propAudioAssetNames.Contains(clipName) && !hunterAudioAssetNames.Contains(clipName))
            {
                PropHuntBase.mls.LogError("Failed to load taunt audio clip. Clip does not exist in the list of valid audio clip names. Clip: " + clipName);
                return null;
            }

            AudioClip audioClip;
            if (loadedAudioClipsDict.TryGetValue(clipName, out audioClip))
                return audioClip;

            try
            {
                audioClip = PropHuntBase.bundle.LoadAsset<AudioClip>(clipName);
                loadedAudioClips.Add(audioClip);
                loadedAudioClipsDict.Add(clipName, audioClip);
                PropHuntBase.mls.LogDebug("Cached audio clip: " + clipName);
            }
            catch
            {
                PropHuntBase.mls.LogError("Failed to load audio clip from asset bundle. Clip: " + clipName);
                return null;
            }

            return audioClip;
        }


        public static void ClearAudioClipCache()
        {
            loadedAudioClips?.Clear();
            loadedAudioClipsDict?.Clear();
        }
    }
}