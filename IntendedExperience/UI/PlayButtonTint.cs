using System;
using System.Collections.Generic;
using System.Reflection;
using HMUI;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace IntendedExperience.UI;

/// <summary>
/// Turns the Play button orange (hue only) when launching will override settings,
/// with a hover hint listing the overrides.
/// </summary>
[UsedImplicitly]
internal class PlayButtonTint : IInitializable, IDisposable, IAffinity
{
    private const float TintHue = 30f / 360f;
    private const string ResampleMethodName = "HandleButtonSelectionStateDidChange";
    private const string HintHeader = "Intended Experience will apply this:";
    private const string SettingsSetterHint =
        "Intended Experience will change settings for this map. You'll see them in the Settings Setter window.";

    // Private in newer game versions, public in 1.29.1.
    private static readonly MethodInfo ResampleMethod = typeof(ButtonStaticAnimations).GetMethod(ResampleMethodName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly PluginConfig config;
    private readonly LaunchPreview launchPreview;
    private readonly StandardLevelDetailViewController levelDetailViewController;
    private readonly DiContainer container;
    private readonly List<ImageColors> originalColors = new();

    private ButtonStaticAnimations? buttonAnimations;
    private NoTransitionsButton? actionButton;
    private HoverHint? hoverHint;

    private bool IsTinted => config.TintPlayButton && !launchPreview.Plan.IsEmpty;

    public PlayButtonTint(PluginConfig config, LaunchPreview launchPreview,
        StandardLevelDetailViewController levelDetailViewController, DiContainer container)
    {
        this.config = config;
        this.launchPreview = launchPreview;
        this.levelDetailViewController = levelDetailViewController;
        this.container = container;
    }

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

        foreach (var imageView in buttonAnimations.GetComponentsInChildren<ImageView>(true))
            originalColors.Add(new ImageColors(imageView));

        hoverHint = container.InstantiateComponent<HoverHint>(actionButton.gameObject);
        return true;
    }

    // The state clips don't key every color (e.g. the hover border's), so restore them all before sampling.
    [AffinityPatch(typeof(ButtonStaticAnimations), ResampleMethodName)]
    [UsedImplicitly]
    [AffinityPrefix]
    private void AnimationSampledPrefix(ButtonStaticAnimations __instance)
    {
        if (__instance != buttonAnimations)
            return;

        foreach (var colors in originalColors)
            colors.Restore();
    }

    [AffinityPatch(typeof(ButtonStaticAnimations), ResampleMethodName)]
    [UsedImplicitly]
    [AffinityPostfix]
    private void AnimationSampledPostfix(ButtonStaticAnimations __instance)
    {
        if (__instance != buttonAnimations || hoverHint == null)
            return;

        // Also runs when the menu reappears, so settings changed meanwhile apply here.
        // A disabled hint gets no pointer events, so it only shows while the button is tinted.
        hoverHint.enabled = IsTinted && launchPreview.ShowTooltips;
        hoverHint.text = launchPreview.ShownInSettingsSetter
            ? SettingsSetterHint
            : $"{HintHeader}\n{launchPreview.Plan.DescribeChanges()}";

        if (!IsTinted)
            return;

        foreach (var colors in originalColors)
            colors.ApplyTint();
    }

    private static Color WithTintHue(Color color)
    {
        Color.RGBToHSV(color, out _, out float saturation, out float value);
        var tinted = Color.HSVToRGB(TintHue, saturation, value);
        tinted.a = color.a;
        return tinted;
    }

    /// <summary>An image of the button with its untinted colors.</summary>
    private readonly record struct ImageColors(ImageView ImageView, Color Color, Color Color0, Color Color1)
    {
        public ImageColors(ImageView imageView) : this(imageView, imageView.color, imageView.color0, imageView.color1) { }

        public void Restore()
        {
            ImageView.color = Color;
            ImageView.color0 = Color0;
            ImageView.color1 = Color1;
        }

        public void ApplyTint()
        {
            ImageView.color = WithTintHue(ImageView.color);
            ImageView.color0 = WithTintHue(ImageView.color0);
            ImageView.color1 = WithTintHue(ImageView.color1);
        }
    }
}
