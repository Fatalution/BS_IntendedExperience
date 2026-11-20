using System;
using System.Reflection;
using HMUI;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>
/// Turns the Play button orange (hue only) when launching will override settings,
/// with a hover hint listing the overrides.
/// </summary>
internal class PlayButtonTint : IInitializable, IDisposable, IAffinity
{
    private const float TintHue = 30f / 360f;
    private const string ResampleMethodName = "HandleButtonSelectionStateDidChange";
    private const string HintHeader = "Intended Experience will apply for this map:";

    private static readonly MethodInfo ResampleMethod =
        typeof(ButtonStaticAnimations).GetMethod(ResampleMethodName, BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly PluginConfig config;
    private readonly LaunchPreview launchPreview;
    private readonly StandardLevelDetailViewController levelDetailViewController;
    private readonly DiContainer container;

    private ButtonStaticAnimations? buttonAnimations;
    private NoTransitionsButton? actionButton;
    private HoverHint? hoverHint;

    public PlayButtonTint(PluginConfig config, LaunchPreview launchPreview,
        StandardLevelDetailViewController levelDetailViewController, DiContainer container)
    {
        this.config = config;
        this.launchPreview = launchPreview;
        this.levelDetailViewController = levelDetailViewController;
        this.container = container;
    }

    private bool IsTinted => config.TintPlayButton && !launchPreview.Plan.IsEmpty;

    public void Initialize() => launchPreview.Changed += Refresh;

    public void Dispose() => launchPreview.Changed -= Refresh;

    // Re-sampling the button's state animation restores its colors; the postfix then tints them if needed.
    private void Refresh()
    {
        if (FindActionButton())
            ResampleMethod.Invoke(buttonAnimations, new object[] { actionButton!.selectionState });
    }

    private bool FindActionButton()
    {
        if (hoverHint != null)
            return true;

        var levelDetailView = levelDetailViewController.GetComponentInChildren<StandardLevelDetailView>(true);
        actionButton = levelDetailView.actionButton as NoTransitionsButton;
        buttonAnimations = levelDetailView.actionButton.GetComponentInChildren<ButtonStaticAnimations>(true);
        if (buttonAnimations == null || actionButton == null)
        {
            Plugin.Log.Warn("Play button animations not found; tint disabled.");
            return false;
        }

        hoverHint = container.InstantiateComponent<HoverHint>(actionButton.gameObject);
        return true;
    }

    [AffinityPatch(typeof(ButtonStaticAnimations), ResampleMethodName)]
    [AffinityPostfix]
    private void AnimationSampledPostfix(ButtonStaticAnimations __instance)
    {
        if (__instance != buttonAnimations || hoverHint == null)
            return;

        // Also runs when the menu reappears, so settings changed meanwhile apply here.
        // A disabled hint gets no pointer events, so it only shows while the button is tinted.
        hoverHint.enabled = IsTinted && config.ShowPlayButtonTooltip;
        hoverHint.text = $"{HintHeader}\n{launchPreview.Plan.DescribeChanges()}";

        if (!IsTinted)
            return;

        foreach (var imageView in __instance.GetComponentsInChildren<ImageView>(true))
        {
            imageView.color = WithTintHue(imageView.color);
            imageView.color0 = WithTintHue(imageView.color0);
            imageView.color1 = WithTintHue(imageView.color1);
        }
    }

    private static Color WithTintHue(Color color)
    {
        Color.RGBToHSV(color, out _, out float saturation, out float value);
        var tinted = Color.HSVToRGB(TintHue, saturation, value);
        tinted.a = color.a;
        return tinted;
    }
}
