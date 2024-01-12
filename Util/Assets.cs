﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FacilityMeltdown {
    internal class Assets {
        internal static AssetBundle assetBundle;

        internal static AudioClip[] warnings;
        internal static AudioClip music, shockwave;
        internal static GameObject facilityExplosionPrefab, shockwavePrefab;
        internal static GameObject[] facilityEffects;
        public static void Init() {
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "facilitymeltdown"));

            warnings = new AudioClip[] {
                assetBundle.LoadAsset<AudioClip>("warning1.mp3"),
                assetBundle.LoadAsset<AudioClip>("warning2.mp3"),
                assetBundle.LoadAsset<AudioClip>("warning3.mp3"),
                assetBundle.LoadAsset<AudioClip>("warning4.mp3")
            };

            music = assetBundle.LoadAsset<AudioClip>("music.mp3");
            shockwave = assetBundle.LoadAsset<AudioClip>("shockwave.mp3");

            facilityExplosionPrefab = assetBundle.LoadAsset<GameObject>("FacilityExplosion.prefab");
            shockwavePrefab = assetBundle.LoadAsset<GameObject>("Shockwave.prefab");

            facilityEffects = new GameObject[] {
                assetBundle.LoadAsset<GameObject>("Dust.prefab"),
                assetBundle.LoadAsset<GameObject>("Waterstream.prefab")
            };
        }
    }
}