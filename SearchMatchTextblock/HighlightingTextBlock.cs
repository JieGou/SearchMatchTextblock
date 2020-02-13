using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SearchMatchTextblock
{
    [TemplatePart(Name = HighlighttextblockName, Type = typeof(TextBlock))]
    public class HighlightingTextBlock : Control
    {
        private const string HighlighttextblockName = "PART_HighlightTextblock";

        private static readonly DependencyPropertyKey MatchCountPropertyKey
            = DependencyProperty.RegisterReadOnly("MatchCount", typeof(int), typeof(HighlightingTextBlock),
                new PropertyMetadata(0));

        public static readonly DependencyProperty MatchCountProperty
            = MatchCountPropertyKey.DependencyProperty;

        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.Register("HighlightText", typeof(string), typeof(HighlightingTextBlock),
                new PropertyMetadata(string.Empty, OnHighlightTextPropertyChanged));

        public static readonly DependencyProperty TextProperty =
            TextBlock.TextProperty.AddOwner(
                typeof(HighlightingTextBlock),
                new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public static readonly DependencyProperty TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner(
            typeof(HighlightingTextBlock),
            new PropertyMetadata(TextWrapping.NoWrap));

        public static readonly DependencyProperty TextTrimmingProperty = TextBlock.TextTrimmingProperty.AddOwner(
            typeof(HighlightingTextBlock),
            new PropertyMetadata(TextTrimming.None));

        public static readonly DependencyProperty HighlightForegroundProperty =
            DependencyProperty.Register("HighlightForeground", typeof(Brush),
                typeof(HighlightingTextBlock),
                new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty HighlightBackgroundProperty =
            DependencyProperty.Register("HighlightBackground", typeof(Brush),
                typeof(HighlightingTextBlock),
                new PropertyMetadata(Brushes.Blue));

        private TextBlock highlightTextBlock;

        static HighlightingTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HighlightingTextBlock),
                new FrameworkPropertyMetadata(typeof(HighlightingTextBlock)));
        }

        public int MatchCount
        {
            get => (int)GetValue(MatchCountProperty);
            protected set => SetValue(MatchCountPropertyKey, value);
        }

        public Brush HighlightBackground
        {
            get => (Brush)GetValue(HighlightBackgroundProperty);
            set => SetValue(HighlightBackgroundProperty, value);
        }

        public Brush HighlightForeground
        {
            get => (Brush)GetValue(HighlightForegroundProperty);
            set => SetValue(HighlightForegroundProperty, value);
        }

        public string HighlightText
        {
            get => (string)GetValue(HighlightTextProperty);
            set => SetValue(HighlightTextProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        private static void OnHighlightTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textblock = (HighlightingTextBlock)d;
            textblock.ProcessTextChanged(textblock.Text, e.NewValue as string);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textblock = (HighlightingTextBlock)d;
            textblock.ProcessTextChanged(e.NewValue as string, textblock.HighlightText);
        }

        private void ProcessTextChanged(string mainText, string highlightText)
        {
            if (highlightTextBlock == null)
            {
                return;
            }

            highlightTextBlock.Inlines.Clear();
            SetValue(MatchCountPropertyKey, 0);
            if (highlightTextBlock == null || string.IsNullOrWhiteSpace(mainText))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(highlightText))
            {
                var completeRun = new Run(mainText);
                highlightTextBlock.Inlines.Add(completeRun);
                return;
            }

            if (highlightText.Contains(" ") || highlightText.Contains(","))
            {
                var textArray = highlightText.Split(new string[] { " ", "," }
                    , StringSplitOptions.RemoveEmptyEntries);
                ColorBrushTextParts(mainText, textArray.ToList());
            }
            else
            {
                ColorBrushTextParts(mainText, new List<string>() { highlightText });
            }
        }

        /// <summary>
        /// 对匹配的文字着色
        /// </summary>
        /// <param name="mainText">文字</param>
        /// <param name="highlightText">匹配的需要亮显的文字</param>
        private void ColorBrushTextPart(string mainText, string highlightText)
        {
            var find = 0;
            var searchTextLength = highlightText.Length;
            while (true)
            {
                var oldFind = find;
                find = mainText.IndexOf(highlightText, find, StringComparison.InvariantCultureIgnoreCase);
                if (find == -1)
                {
                    highlightTextBlock.Inlines.Add(
                        oldFind > 0
                            ? GetRunForText(mainText.Substring(oldFind, mainText.Length - oldFind), false)
                            : GetRunForText(mainText, false));
                    break;
                }

                if (oldFind == find)
                {
                    highlightTextBlock.Inlines.Add(GetRunForText(mainText.Substring(oldFind, searchTextLength), true));
                    SetValue(MatchCountPropertyKey, MatchCount + 1);
                    find = find + searchTextLength;
                    continue;
                }

                highlightTextBlock.Inlines.Add(GetRunForText(mainText.Substring(oldFind, find - oldFind), false));
            }
        }

        /// <summary>
        /// 对匹配的文字列表着色 即多个条件
        /// </summary>
        /// <param name="mainText">文字</param>
        /// <param name="highlightTextList">匹配的需要亮显的文字</param>
        private void ColorBrushTextParts(string mainText, List<string> highlightTextList)
        {
            if (highlightTextList.Count == 1)
            {
                ColorBrushTextPart(mainText, highlightTextList[0]);
                return;
            }

            var find = 0;
            while (true)
            {
                //能匹配的 需要亮显的字符串
                var canBeMatchTextList =
                    (from item in highlightTextList
                     let index = mainText.IndexOf(item, find, StringComparison.InvariantCultureIgnoreCase)
                     where index >= 0
                     orderby index
                     select item)
                    .ToList();
                if (canBeMatchTextList.Count == 0)
                {
                    break;
                }

                string highlightText = canBeMatchTextList.First();
                var searchTextLength = highlightText.Length;

                var oldFind = find;
                find = mainText.IndexOf(highlightText, find, StringComparison.InvariantCultureIgnoreCase);
                if (find == -1)
                {
                    highlightTextBlock.Inlines.Add(
                        oldFind > 0
                            ? GetRunForText(mainText.Substring(oldFind, mainText.Length - oldFind), false)
                            : GetRunForText(mainText, false));
                    break;
                }

                if (oldFind == find)
                {
                    highlightTextBlock.Inlines.Add(GetRunForText(mainText.Substring(oldFind, searchTextLength), true));
                    SetValue(MatchCountPropertyKey, MatchCount + 1);
                    find = find + searchTextLength;
                    continue;
                }

                highlightTextBlock.Inlines.Add(GetRunForText(mainText.Substring(oldFind, find - oldFind), false));
            }
        }

        /// <summary>
        /// 为匹配的文字部分添加着色
        /// </summary>
        /// <param name="text">匹配的文字部分</param>
        /// <param name="isHighlighted">是否亮显（着色）</param>
        /// <returns></returns>
        private Run GetRunForText(string text, bool isHighlighted)
        {
            var textRun = new Run(text)
            {
                Foreground = isHighlighted ? HighlightForeground : Foreground,
                Background = isHighlighted ? HighlightBackground : Background
            };
            return textRun;
        }

        public override void OnApplyTemplate()
        {
            highlightTextBlock = GetTemplateChild(HighlighttextblockName) as TextBlock;
            if (highlightTextBlock == null)
            {
                return;
            }

            ProcessTextChanged(Text, HighlightText);
        }
    }
}