using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionPlugin;
using System.IO;
using System.Threading;
using System.Globalization;

namespace ExamplePlugin
{
    public class ExamplePlugin : IPlugin
    {
        public string Name => "BeatSaberSongStatus";
        public string Version => "2.0.0";

        private readonly string dir = Path.Combine(Environment.CurrentDirectory, "status.txt");
        private readonly string templateDir = Path.Combine(Environment.CurrentDirectory, "statusTemplate.txt");

        private readonly string[] env = { "DefaultEnvironment", "BigMirrorEnvironment", "TriangleEnvironment", "NiceEnvironment" };
        private readonly string defaultTemplate = string.Join(
            Environment.NewLine,
            "Playing: {songName}{ songSubName} - {authorName}",
            "{gamemode} | {difficulty} | BPM: {beatsPerMinute}",
            "{[isNoFail] }{[isMirrored] }");

        HMTask getData;
        Action getDataJob;
        
        private MainGameSceneSetupData setupData;

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            
            if (!File.Exists(templateDir))
                File.WriteAllText(templateDir, defaultTemplate);
        }
        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (env.Contains(arg1.name))
                StartData();
            else if (arg1.name == "Menu")
            {
                File.WriteAllText(dir, "");
                getData.Cancel();
                setupData = null;
            }
        }
        
        private void StartData()
        {
            getDataJob = delegate
            {
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(150);
                    setupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().FirstOrDefault();
                    if (setupData != null) break;
                }
                if (setupData == null)
                {
                    Console.WriteLine("[SongStatus] Couldn't find SetupData object.");
                    return;
                }

                if (!File.Exists(templateDir))
                    File.WriteAllText(templateDir, defaultTemplate);
                string temp = File.ReadAllText(templateDir);

                var diff = setupData.difficultyLevel;
                var song = diff.level;
                string mode = GetGameplayModeName(setupData.gameplayMode);

                var keywords = temp.Split('{', '}');

                temp = ReplaceKeyword("songName", song.songName, keywords, temp);
                temp = ReplaceKeyword("songSubName", song.songSubName, keywords, temp);
                temp = ReplaceKeyword("authorName", song.songAuthorName, keywords, temp);
                temp = ReplaceKeyword("gamemode", mode, keywords, temp);
                temp = ReplaceKeyword("difficulty", diff.difficulty.Name(), keywords, temp);
                temp = ReplaceKeyword("isNoFail",
                    setupData.gameplayOptions.noEnergy ? "No Fail" : string.Empty, keywords, temp);
                temp = ReplaceKeyword("isMirrored",
                    setupData.gameplayOptions.mirror ? "Mirrored" : string.Empty, keywords, temp);
                temp = ReplaceKeyword("beatsPerMinute",
                    song.beatsPerMinute.ToString(CultureInfo.InvariantCulture), keywords, temp);
                temp = ReplaceKeyword("notesCount",
                    diff.beatmapData.notesCount.ToString(CultureInfo.InvariantCulture), keywords, temp);
                temp = ReplaceKeyword("obstaclesCount",
                    diff.beatmapData.obstaclesCount.ToString(CultureInfo.InvariantCulture), keywords, temp);
                temp = ReplaceKeyword("environmentName", song.environmentSceneInfo.sceneName, keywords,
                    temp);

                File.WriteAllText(dir, temp);
            };
            getData = new HMTask(getDataJob);
            getData.Run();
        }

        private static string GetGameplayModeName(GameplayMode gameplayMode)
        {
            switch (gameplayMode)
            {
                case GameplayMode.SoloStandard:
                    return "Solo Standard";
                case GameplayMode.SoloOneSaber:
                    return "One Saber";
                case GameplayMode.SoloNoArrows:
                    return "No Arrows";
                case GameplayMode.PartyStandard:
                    return "Party";
                default:
                    return "Solo Standard";
            }
        }
        private string ReplaceKeyword(string keyword, string replaceKeyword, string[] keywords, string text)
        {
            if (!keywords.Any(x => x.Contains(keyword))) return text;
            var containingKeywords = keywords.Where(x => x.Contains(keyword));

            if (string.IsNullOrEmpty(replaceKeyword))
            {
                foreach (var containingKeyword in containingKeywords)
                    text = text.Replace("{" + containingKeyword + "}", string.Empty);

                return text;
            }

            foreach (var containingKeyword in containingKeywords)
                text = text.Replace("{" + containingKeyword + "}", containingKeyword);

            text = text.Replace(keyword, replaceKeyword);

            return text;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1) { }
        public void OnLevelWasLoaded(int level) { }
        public void OnLevelWasInitialized(int level) { }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
    }
}