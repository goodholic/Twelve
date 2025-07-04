﻿using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.DAI;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if TextMeshPro && RTLTMP_EXISTS
using RTLTMPro;
#endif

#if TextMeshPro
using TMPro;
#endif

#if NOVA_UI_EXISTS
using Nova.TMP;
#endif

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class TextMeshDrawer : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        [SerializeField] List<FObject> texts;
        public List<FObject> Texts => texts;

        public void DrawRTL(FObject fobject)
        {
#if TextMeshPro && RTLTMP_EXISTS
            fobject.Data.GameObject.TryAddGraphic(out RTLTextMeshPro text);

            text.Farsi = monoBeh.Settings.TextMeshSettings.Farsi;
            text.ForceFix = monoBeh.Settings.TextMeshSettings.ForceFix;
            text.PreserveNumbers = monoBeh.Settings.TextMeshSettings.PreserveNumbers;
            text.FixTags = monoBeh.Settings.TextMeshSettings.FixTags;

            Draw(fobject, text);
#endif
        }

        public void DrawTMP(FObject fobject)
        {
#if TextMeshPro
            TMP_Text text;

            fobject.Data.GameObject.TryAddGraphic(out TextMeshProUGUI uguiText);
            text = uguiText;

            Draw(fobject, text);
#endif
        }

        public void DrawNovaTMP(FObject fobject)
        {
#if TextMeshPro && NOVA_UI_EXISTS
            TMP_Text text;

            fobject.Data.GameObject.TryAddGraphic(out TextMeshProTextBlock novaText);
            text = novaText;

            Draw(fobject, text);
#endif
        }

#if TextMeshPro
        private void Draw(FObject fobject, TMP_Text text)
        {
            string str = fobject.GetText();

            text.text = str;

            text.overrideColorTags = monoBeh.Settings.TextMeshSettings.OverrideTags;
            text.enableAutoSizing = monoBeh.Settings.TextMeshSettings.AutoSize;

#if UNITY_2023_1_OR_NEWER
            if (monoBeh.Settings.TextMeshSettings.Wrapping)
            {
                text.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
#else
            text.enableWordWrapping = monoBeh.Settings.TextMeshSettings.Wrapping;
#endif

            if (monoBeh.IsNova())
            {
                text.isOrthographic = monoBeh.Settings.TextMeshSettings.OrthographicMode;
            }

            text.richText = monoBeh.Settings.TextMeshSettings.RichText;
            text.raycastTarget = monoBeh.Settings.TextMeshSettings.RaycastTarget;
            text.parseCtrlCharacters = monoBeh.Settings.TextMeshSettings.ParseEscapeCharacters;
            text.useMaxVisibleDescender = monoBeh.Settings.TextMeshSettings.VisibleDescender;

#if UNITY_6000_0_OR_NEWER
            var fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>();
            if (monoBeh.Settings.TextMeshSettings.Kerning)
            {
                fontFeatures.Add(UnityEngine.TextCore.OTL_FeatureTag.kern);
            }
            text.fontFeatures = fontFeatures;
#elif UNITY_2023_4_OR_NEWER == false
            text.enableKerning = monoBeh.Settings.TextMeshSettings.Kerning;
#endif

            text.extraPadding = monoBeh.Settings.TextMeshSettings.ExtraPadding;
            text.horizontalMapping = monoBeh.Settings.TextMeshSettings.HorizontalMapping;
            text.verticalMapping = monoBeh.Settings.TextMeshSettings.VerticalMapping;
            text.geometrySortingOrder = monoBeh.Settings.TextMeshSettings.GeometrySorting;

            if (monoBeh.Settings.TextMeshSettings.Shader != null)
            {
                text.fontMaterial.shader = monoBeh.Settings.TextMeshSettings.Shader;
            }

            SetFont(text, fobject);

            text.alignment = fobject.GetTextAnchor().ToTextMeshAnchor();

            SetFontSize(text, fobject);
            SetFontCase(text, fobject);
            SetOverflowMode(text, fobject);
            SetColor(text, fobject);
        }

        private const float _sizeFixForNonOrthographicNovaText = 10f;

        private void SetFontSize(TMP_Text text, FObject fobject)
        {
            if (monoBeh.Settings.TextMeshSettings.AutoSize)
            {
                text.fontSizeMin = 1;
                text.fontSizeMax = fobject.Style.FontSize;
            }
            else
            {
                text.fontSize = fobject.Style.FontSize;
            }

            if (monoBeh.IsNova())
            {
                if (monoBeh.Settings.TextMeshSettings.OrthographicMode == false)
                {
                    text.fontSize *= _sizeFixForNonOrthographicNovaText;
                }
            }

            if (!fobject.Style.FontFamily.IsEmpty())
            {
                text.characterSpacing = (float)Math.Round(fobject.Style.LetterSpacing / text.fontSize * 100, FcuConfig.Rounding.FontDigits);
                text.lineSpacing = (float)Math.Round(fobject.Style.LineHeightPx / fobject.Style.FontSize * 100 - text.font.faceInfo.lineHeight, FcuConfig.Rounding.FontDigits);
                text.paragraphSpacing = (float)Math.Round(fobject.Style.ParagraphSpacing / fobject.Style.FontSize * 100, FcuConfig.Rounding.FontDigits);
            }
        }

        private void SetFont(TMP_Text text, FObject fobject)
        {
            TMP_FontAsset font = monoBeh.FontLoader.GetFontFromArray(fobject, monoBeh.FontLoader.TmpFonts);
            text.font = font;
        }

        private void SetFontCase(TMP_Text text, FObject fobject)
        {
            FontStyles textDecoration = FontStyles.Normal;
            FontStyles textCase = FontStyles.Normal;
            FontStyles textItalic = FontStyles.Normal;
            FontStyles textBold = FontStyles.Normal;

            if (fobject.Data.HasFontAsset == false)
            {
                if (fobject.Style.Italic.ToBoolNullFalse())
                {
                    textItalic = FontStyles.Italic;
                }

                if (fobject.Style.FontWeight > 600)
                {
                    textBold = FontStyles.Bold;
                }
            }

            switch (fobject.Style.TextDecoration)
            {
                case "UNDERLINE":
                    textDecoration = FontStyles.Underline;
                    break;
                case "STRIKETHROUGH":
                    textDecoration = FontStyles.Strikethrough;
                    break;
            }

            switch (fobject.Style.TextCase)
            {
                case "UPPER":
                    textCase = FontStyles.UpperCase;
                    break;
                case "LOWER":
                    textCase = FontStyles.LowerCase;
                    break;
                case "TITLE":
                    textCase = FontStyles.Normal;
                    break;
                case "SMALL_CAPS":
                    textCase = FontStyles.SmallCaps;
                    break;
            }

            FontStyles final = textDecoration | textCase | textItalic | textBold;

            text.fontStyle = final;
        }

        private void SetOverflowMode(TMP_Text text, FObject fobject)
        {
            TextOverflowModes textTurncate = monoBeh.Settings.TextMeshSettings.Overflow;

            if (fobject.Style.TextAutoResize == TextAutoResize.NONE || fobject.Style.TextAutoResize == TextAutoResize.TRUNCATE)
            {
                switch (fobject.Style.TextCase)
                {
                    case "ENDING":
                        textTurncate = TextOverflowModes.Ellipsis;
                        break;
                }
            }

            text.overflowMode = textTurncate;
        }

        private void SetColor(TMP_Text text, FObject fobject)
        {
            FGraphic graphic = fobject.Data.Graphic;

            text.enableVertexGradient = false;

            if (graphic.Fill.HasSolid)
            {
                text.color = graphic.Fill.SolidPaint.Color;
            }
            else if (graphic.Fill.HasGradient)
            {
                List<GradientColorKey> gradientColorKeys = graphic.Fill.GradientPaint.ToGradientColorKeys();

                if (!gradientColorKeys.IsEmpty())
                {
                    text.color = gradientColorKeys.First().color;
                }
            }

            if (graphic.HasStroke && fobject.StrokeAlign == StrokeAlign.INSIDE)
            {
                float normalizedWidth = fobject.StrokeWeight / text.preferredHeight;
                text.outlineWidth = normalizedWidth;

                if (graphic.Stroke.HasSolid)
                {
                    text.outlineColor = graphic.Stroke.SolidPaint.Color;
                }
                else if (graphic.Stroke.HasGradient)
                {
                    List<GradientColorKey> gradientColorKeys = graphic.Stroke.GradientPaint.ToGradientColorKeys();

                    if (!gradientColorKeys.IsEmpty())
                    {
                        text.outlineColor = gradientColorKeys.First().color;
                    }
                }
            }
            else
            {
                text.outlineWidth = 0;
            }
        }
#endif
    }
}
