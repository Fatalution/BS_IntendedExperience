using System;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using IntendedExperience.Detection;
using IntendedExperience.Settings;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IntendedExperience.UI;

internal class IntendedPlayButton : IInitializable, IDisposable
{
    private const string ResourcePath = "IntendedExperience.UI.IntendedPlayButton.bsml";

    private readonly MapAnalyzer _mapAnalyzer;
    private readonly PlayerSettingsManager _settingsManager;
    private readonly SettingsApplier _settingsApplier;
    private readonly StandardLevelDetailViewController _levelDetailViewController;

    private FloatingScreen? _floatingScreen;
    private BeatmapLevel? _currentLevel;
    private BeatmapKey _currentBeatmapKey;

    public IntendedPlayButton(
        MapAnalyzer mapAnalyzer,
        PlayerSettingsManager settingsManager,
        SettingsApplier settingsApplier,
        StandardLevelDetailViewController levelDetailViewController)
    {
        _mapAnalyzer = mapAnalyzer;
        _settingsManager = settingsManager;
        _settingsApplier = settingsApplier;
        _levelDetailViewController = levelDetailViewController;
    }

    public void Initialize()
    {
        Plugin.Log.Debug("IntendedPlayButton: Initializing");

        try
        {
            CreateFloatingButton();
            SubscribeToEvents();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"IntendedPlayButton: Failed to initialize: {ex}");
        }
    }

    public void Dispose()
    {
        Plugin.Log.Debug("IntendedPlayButton: Disposing");

        UnsubscribeFromEvents();

        if (_floatingScreen != null)
        {
            UnityEngine.Object.Destroy(_floatingScreen.gameObject);
            _floatingScreen = null;
        }
    }

    private void CreateFloatingButton()
    {
        // Create a floating screen to host our button
        _floatingScreen = FloatingScreen.CreateFloatingScreen(
            new Vector2(40f, 10f),
            false,
            new Vector3(0f, 0f, 0f),
            Quaternion.identity);

        _floatingScreen.gameObject.name = "IntendedExperienceButton";

        // Parse the BSML and attach it to the floating screen
        BSMLParser.Instance.Parse(
            Utilities.GetResourceContent(typeof(IntendedPlayButton).Assembly, ResourcePath),
            _floatingScreen.gameObject,
            this);

        // Initially hide the button until a level is selected
        _floatingScreen.gameObject.SetActive(false);

        // Parent to the level detail view so it moves with it
        var levelDetailTransform = _levelDetailViewController.transform;
        _floatingScreen.transform.SetParent(levelDetailTransform, false);

        // Position the button near the play button
        _floatingScreen.transform.localPosition = new Vector3(28f, -25f, 0f);
        _floatingScreen.transform.localScale = Vector3.one;

        Plugin.Log.Debug("IntendedPlayButton: Floating button created");
    }

    private void SubscribeToEvents()
    {
        _levelDetailViewController.didChangeContentEvent += OnContentChanged;
    }

    private void UnsubscribeFromEvents()
    {
        _levelDetailViewController.didChangeContentEvent -= OnContentChanged;
    }

    private void OnContentChanged(StandardLevelDetailViewController controller,
        StandardLevelDetailViewController.ContentType contentType)
    {
        if (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady)
        {
            UpdateButtonState();
        }
        else
        {
            HideButton();
        }
    }

    private void UpdateButtonState()
    {
        try
        {
            _currentLevel = _levelDetailViewController.beatmapLevel;
            _currentBeatmapKey = _levelDetailViewController.beatmapKey;

            if (_currentLevel == null)
            {
                HideButton();
                return;
            }

            // Analyze the map to see if our button is relevant
            var mapInfo = _mapAnalyzer.Analyze(_currentLevel, _currentBeatmapKey);

            // Show button if we would actually change any settings
            var shouldShow = !mapInfo.IsV2OrV2Environment;

            if (shouldShow && _floatingScreen != null)
            {
                _floatingScreen.gameObject.SetActive(true);
                Plugin.Log.Debug($"IntendedPlayButton: Showing button for {_currentLevel.songName}");
            }
            else
            {
                HideButton();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"IntendedPlayButton: Failed to update button state: {ex}");
            HideButton();
        }
    }

    private void HideButton()
    {
        if (_floatingScreen != null)
        {
            _floatingScreen.gameObject.SetActive(false);
        }
    }

    [UIAction("OnIntendedPlayClick")]
    private void OnIntendedPlayClick()
    {
        Plugin.Log.Info("IntendedPlayButton: Play (Intended) clicked");

        if (_currentLevel == null)
        {
            Plugin.Log.Warn("IntendedPlayButton: No level selected");
            return;
        }

        try
        {
            // 1. Create a settings snapshot
            _settingsManager.CreateSnapshot();

            // 2. Analyze the map
            var mapInfo = _mapAnalyzer.Analyze(_currentLevel, _currentBeatmapKey);

            // 3. Apply appropriate settings
            _settingsApplier.ApplySettings(mapInfo);

            // 4. Start the level - invoke the play action
            StartLevel();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"IntendedPlayButton: Failed to start with intended settings: {ex}");

            // If something went wrong, try to restore the snapshot
            if (_settingsManager.HasSavedSnapshot)
            {
                _settingsManager.RestoreSnapshot();
            }
        }
    }

    private void StartLevel()
    {
        try
        {
            // Find and invoke the play button in the StandardLevelDetailView
            // The view contains an action button that triggers gameplay
            var buttons = _levelDetailViewController.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                // Look for the action/play button by name
                var buttonName = button.gameObject.name.ToLowerInvariant();
                if (buttonName.Contains("action") || buttonName.Contains("play"))
                {
                    button.onClick?.Invoke();
                    Plugin.Log.Debug($"IntendedPlayButton: Invoked button: {button.gameObject.name}");
                    return;
                }
            }

            // Alternative: Try using reflection to call the play method
            var playMethod = _levelDetailViewController.GetType().GetMethod(
                "ActionButtonWasPressed",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (playMethod != null)
            {
                playMethod.Invoke(_levelDetailViewController, null);
                return;
            }

            Plugin.Log.Warn("IntendedPlayButton: Could not find play button to invoke");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"IntendedPlayButton: Failed to start level: {ex}");
        }
    }
}
