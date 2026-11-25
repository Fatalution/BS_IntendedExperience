using TMPro;
using UnityEngine;

namespace IntendedExperience.UI;

/// <summary>Centres a left-aligned text's lines as one block by padding its left edge.</summary>
[RequireComponent(typeof(TextMeshProUGUI))]
internal class CenteredTextBlock : MonoBehaviour
{
    private TextMeshProUGUI? text;

    /// <summary>Re-centres the block; call after the text changes.</summary>
    public void Refresh()
    {
        if (text == null)
            return;

        // Measured without padding: the preferred width includes the current margin.
        text.margin = Vector4.zero;
        float blockWidth = text.GetPreferredValues(text.text).x;
        float leftPadding = Mathf.Max(0f, (text.rectTransform.rect.width - blockWidth) / 2f);
        text.margin = new Vector4(leftPadding, 0f, 0f, 0f);
    }

    private void Awake() => text = GetComponent<TextMeshProUGUI>();

    private void OnEnable() => Refresh();

    private void OnRectTransformDimensionsChange() => Refresh();
}
