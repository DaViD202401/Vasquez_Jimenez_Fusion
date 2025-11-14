using Fusion;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIHitBoard : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private TextMeshProUGUI entryPrefab;
    [SerializeField] private float refreshInterval = 0.2f;

    private float _timer;
    private readonly List<TextMeshProUGUI> _entries = new List<TextMeshProUGUI>();

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        _timer = refreshInterval;
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        Player[] players;

#if UNITY_2023_1_OR_NEWER
        players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
#else
        players = Object.FindObjectsOfType<Player>();
#endif

        if (players == null || players.Length == 0)
        {
            ClearEntries();
            return;
        }

        System.Array.Sort(players, (a, b) => b.HitCount.CompareTo(a.HitCount));

        while (_entries.Count > players.Length)
        {
            var last = _entries[_entries.Count - 1];
            _entries.RemoveAt(_entries.Count - 1);
            if (last != null) Destroy(last.gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (i >= _entries.Count)
            {
                var go = Instantiate(entryPrefab, content);
                _entries.Add(go);
            }

            var p = players[i];
            string idLabel = GetPlayerLabel(p);
            int hits = p.HitCount;

            _entries[i].text = $"{i + 1}. {idLabel} — {hits}";

            var rend = p.GetComponentInChildren<MeshRenderer>();
            if (rend != null && rend.material != null)
                _entries[i].color = rend.material.color;
            else
                _entries[i].color = Color.white;
        }
    }

    private string GetPlayerLabel(Player p)
    {
        if (p.Object != null)
        {
            try
            {
                var auth = p.Object.InputAuthority;
                if (auth != null)
                    return auth.ToString();
            }
            catch { }
        }
        return p.gameObject.name;
    }

    private void ClearEntries()
    {
        foreach (var e in _entries)
            if (e != null) Destroy(e.gameObject);
        _entries.Clear();
    }
}