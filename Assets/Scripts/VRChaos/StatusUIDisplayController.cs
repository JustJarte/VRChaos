using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusUIDisplayController : NetworkBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private RectTransform stunnedIcon;
    [SerializeField] private RectTransform slowedIcon;
    [SerializeField] private RectTransform invulnerableIcon;
    [SerializeField] private RectTransform buffedIcon;
    [SerializeField] private RectTransform eliminatedIcon;

    [SerializeField] private float iconSpacing = 20.0f;

    private List<RectTransform> allIcons;

    public override void Spawned()
    {
        allIcons = new List<RectTransform>
        {
            stunnedIcon,
            slowedIcon,
            invulnerableIcon,
            buffedIcon,
            eliminatedIcon
        };
    }

    public override void FixedUpdateNetwork()
    {
        if (player == null) return;

        stunnedIcon.gameObject.SetActive(player.IsStunned);
        slowedIcon.gameObject.SetActive(player.IsSlowed);
        invulnerableIcon.gameObject.SetActive(player.IsInvulnerable);
        buffedIcon.gameObject.SetActive(player.HasBuff);
        eliminatedIcon.gameObject.SetActive(player.IsEliminated);

        UpdateIconLayout();
    }

    private void UpdateIconLayout()
    {
        List<RectTransform> activeIcons = new List<RectTransform>();

        foreach (var icon in allIcons)
        {
            if (icon.gameObject.activeSelf)
            {
                activeIcons.Add(icon);
            }
        }

        int count = activeIcons.Count;

        if (count == 0) return;

        float totalWidth = (count - 1) * iconSpacing;
        float startX = -totalWidth / 2.0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 newPos = new Vector3(startX + i * iconSpacing, 0.479f, 0.0f);
            activeIcons[i].anchoredPosition = newPos;
        }
    }
}
