// Assets/Editor/AudioAutoBinder.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AudioAutoBinder
{
    // -------- Menu --------
    [MenuItem("Tools/Audio/Auto Bind Clips (Only Empty)")]
    public static void BindOnlyEmpty() => Run(overwrite: false);

    [MenuItem("Tools/Audio/Auto Bind Clips (Overwrite All)")]
    public static void BindOverwrite() => Run(overwrite: true);

    // -------- Core --------
    static void Run(bool overwrite)
    {
        int setCount = 0, skipCount = 0;

        // 미리 전체 오디오클립 인덱싱 (이름 소문자 -> AudioClip)
        var clipMap = BuildClipMap();

        // 씬 내 대상 컴포넌트 탐색
        setCount += Bind_PlayerController2D(clipMap, overwrite, ref skipCount);
        setCount += Bind_PlayerDeath(clipMap, overwrite, ref skipCount);
        setCount += Bind_LavaProximityFX(clipMap, overwrite, ref skipCount);
        setCount += Bind_ColorActivationManager(clipMap, overwrite, ref skipCount);
        setCount += Bind_Spike(clipMap, overwrite, ref skipCount);
        setCount += Bind_GameOverUI(clipMap, overwrite, ref skipCount);
        setCount += Bind_UIButtonSFX(clipMap, overwrite, ref skipCount);
        setCount += Bind_BgmController(clipMap, overwrite, ref skipCount);

        // AudioManager 보조: 소스 없으면 자동 생성/연결
        setCount += Ensure_AudioManager_Sources(ref skipCount);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[AudioAutoBinder] Done. Set: {setCount}, Skipped: {skipCount}. (overwrite={overwrite})");
    }

    static Dictionary<string, List<AudioClip>> BuildClipMap()
    {
        var dict = new Dictionary<string, List<AudioClip>>();
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) continue;
            string key = clip.name.ToLowerInvariant();
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<AudioClip>();
                dict[key] = list;
            }
            list.Add(clip);
        }
        return dict;
    }

    static AudioClip FindClipAny(Dictionary<string, List<AudioClip>> map, params string[] keywords)
    {
        foreach (var k in keywords)
        {
            string needle = k.ToLowerInvariant();
            // 키 전체 일치
            foreach (var kv in map)
                if (kv.Key == needle && kv.Value.Count > 0)
                    return kv.Value[0];

            // 부분 일치
            foreach (var kv in map)
                if (kv.Key.Contains(needle) && kv.Value.Count > 0)
                    return kv.Value[0];
        }
        return null;
    }

    // ---------- Binders ----------
    static int Bind_PlayerController2D(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var targets = UnityEngine.Object.FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var sp = so.FindProperty("jumpClip");
            if (sp != null)
            {
                if (overwrite || sp.objectReferenceValue == null)
                {
                    var clip = FindClipAny(map, "jump");
                    if (clip) { sp.objectReferenceValue = clip; so.ApplyModifiedProperties(); EditorUtility.SetDirty(t); set++; }
                    else skip++;
                }
                else skip++;
            }
        }
        return set;
    }

    static int Bind_PlayerDeath(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var targets = UnityEngine.Object.FindObjectsByType<PlayerDeath>(FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var sp = so.FindProperty("deathClip");
            if (sp != null)
            {
                if (overwrite || sp.objectReferenceValue == null)
                {
                    var clip = FindClipAny(map, "death");
                    if (clip) { sp.objectReferenceValue = clip; so.ApplyModifiedProperties(); EditorUtility.SetDirty(t); set++; }
                    else skip++;
                }
                else skip++;
            }
        }
        return set;
    }

    static int Bind_LavaProximityFX(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var targets = UnityEngine.Object.FindObjectsByType<LavaProximityFX>(FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var sp = so.FindProperty("lavaBoilLoop");
            if (sp != null)
            {
                if (overwrite || sp.objectReferenceValue == null)
                {
                    var clip = FindClipAny(map, "lava", "boil");
                    if (clip) { sp.objectReferenceValue = clip; so.ApplyModifiedProperties(); EditorUtility.SetDirty(t); set++; }
                    else skip++;
                }
                else skip++;
            }
        }
        return set;
    }

    static int Bind_ColorActivationManager(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var targets = UnityEngine.Object.FindObjectsByType<ColorActivationManager>(FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var spAct = so.FindProperty("sfxActivate");
            if (spAct != null)
            {
                if (overwrite || spAct.objectReferenceValue == null)
                {
                    var clip = FindClipAny(map, "activate", "toggle", "switch");
                    if (clip) { spAct.objectReferenceValue = clip; so.ApplyModifiedProperties(); EditorUtility.SetDirty(t); set++; }
                    else skip++;
                }
                else skip++;
            }
        }
        return set;
    }

    static int Bind_Spike(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var targets = UnityEngine.Object.FindObjectsByType<Spike>(FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            var sp = so.FindProperty("sfxHit");
            if (sp != null)
            {
                if (overwrite || sp.objectReferenceValue == null)
                {
                    var clip = FindClipAny(map, "hit", "spike", "hurt", "damage");
                    if (clip) { sp.objectReferenceValue = clip; so.ApplyModifiedProperties(); EditorUtility.SetDirty(t); set++; }
                    else skip++;
                }
                else skip++;
            }
        }
        return set;
    }

    static int Bind_GameOverUI(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var targets = UnityEngine.Object.FindObjectsByType<GameOverUI>(FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t);
            set += SetClip(so, "sfxTick", FindClipAny(map, "tick", "count", "beep"), overwrite, ref skip, t);
            set += SetClip(so, "sfxConfirm", FindClipAny(map, "confirm", "ding", "finish"), overwrite, ref skip, t);
            set += SetClip(so, "sfxNewRecord", FindClipAny(map, "newrecord", "record", "fanfare", "cele"), overwrite, ref skip, t);
        }
        return set;
    }

    static int Bind_UIButtonSFX(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        // UIButtonSFX가 프로젝트에 없을 수도 있으니 GetType으로 안전 탐색
        var type = Type.GetType("UIButtonSFX");
        if (type == null) return 0;

        var targets = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t as UnityEngine.Object);
            set += SetClip(so, "clickClip", FindClipAny(map, "click", "ui_click"), overwrite, ref skip, t as UnityEngine.Object);
            set += SetClip(so, "hoverClip", FindClipAny(map, "hover", "ui_hover"), overwrite, ref skip, t as UnityEngine.Object);
        }
        return set;
    }

    static int Bind_BgmController(Dictionary<string, List<AudioClip>> map, bool overwrite, ref int skip)
    {
        int set = 0;
        var type = Type.GetType("BgmController");
        if (type == null) return 0;

        var targets = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
        foreach (var t in targets)
        {
            var so = new SerializedObject(t as UnityEngine.Object);
            set += SetClip(so, "titleClip", FindClipAny(map, "title", "menu"), overwrite, ref skip, t as UnityEngine.Object);
            set += SetClip(so, "inGameClip", FindClipAny(map, "ingame", "game", "stage"), overwrite, ref skip, t as UnityEngine.Object);
        }
        return set;
    }

    static int SetClip(SerializedObject so, string propertyName, AudioClip clip, bool overwrite, ref int skip, UnityEngine.Object target)
    {
        var sp = so.FindProperty(propertyName);
        if (sp == null) { skip++; return 0; }

        if (!overwrite && sp.objectReferenceValue != null) { skip++; return 0; }
        if (!clip) { skip++; return 0; }

        sp.objectReferenceValue = clip;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        return 1;
    }

    // ---------- AudioManager 보조 ----------
    static int Ensure_AudioManager_Sources(ref int skip)
    {
        int set = 0;
        var ams = UnityEngine.Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None);
        foreach (var am in ams)
        {
            var so = new SerializedObject(am);
            var bgm = so.FindProperty("bgmSource");
            var sfx = so.FindProperty("sfxSource");

            bool changed = false;

            if (bgm != null && bgm.objectReferenceValue == null)
            {
                // 자식 중 AudioSource 이름 탐색
                var src = FindChildSource(am.transform, "BGM Source") ?? EnsureChildSource(am.transform, "BGM Source", loop: true);
                bgm.objectReferenceValue = src; changed = true;
            }
            if (sfx != null && sfx.objectReferenceValue == null)
            {
                var src = FindChildSource(am.transform, "SFX Source") ?? EnsureChildSource(am.transform, "SFX Source", loop: false);
                sfx.objectReferenceValue = src; changed = true;
            }

            if (changed) { so.ApplyModifiedProperties(); EditorUtility.SetDirty(am); set++; } else skip++;
        }
        return set;
    }

    static AudioSource FindChildSource(Transform t, string name)
    {
        var child = t.Find(name);
        if (child) return child.GetComponent<AudioSource>();
        return null;
    }

    static AudioSource EnsureChildSource(Transform t, string name, bool loop)
    {
        var child = t.Find(name);
        if (!child)
        {
            var go = new GameObject(name);
            go.transform.SetParent(t, false);
            child = go.transform;
        }
        var src = child.GetComponent<AudioSource>();
        if (!src) src = child.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        return src;
    }
}
#endif
