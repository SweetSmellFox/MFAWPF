// <copyright file="CustomTextBlock.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

using MFAWPF.Helper;
using System.Windows;
using System.Windows.Media;
using MFAWPF.Views;


namespace MFAWPF.Controls
{
    public class CustomTextBlock : System.Windows.Controls.TextBlock
    {
        static CustomTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomTextBlock),
                new FrameworkPropertyMetadata(typeof(CustomTextBlock)));
        }

        public static readonly DependencyProperty ResourceKeyProperty =
            DependencyProperty.Register(
                nameof(ResourceKey),
                typeof(string),
                typeof(CustomTextBlock),
                new FrameworkPropertyMetadata(
                    string.Empty, OnResourceKeyChanged));

        public string ResourceKey
        {
            get => (string)GetValue(ResourceKeyProperty);
            set => SetValue(ResourceKeyProperty, value);
        }

        public CustomTextBlock()
        {
            UpdateText();
            LanguageHelper.LanguageChanged += OnLanguageChanged;
        }

        private void UpdateText()
        {
            if (string.IsNullOrEmpty(ResourceKey))
                return;
            Text = ResourceKey.ToLocalization();
        }

        private static void OnResourceKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnResourceKeyChanged(d, (string)e.NewValue);
        }

        private static void OnResourceKeyChanged(DependencyObject d, string newText)
        {
            CustomTextBlock text = (CustomTextBlock)d;
            text.UpdateText();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        public static readonly DependencyProperty CustomForegroundProperty =
            DependencyProperty.Register(nameof(CustomForeground), typeof(Brush), typeof(CustomTextBlock),
                new PropertyMetadata(Brushes.Gray));

        public Brush CustomForeground
        {
            get => (Brush)GetValue(CustomForegroundProperty);
            set => SetValue(CustomForegroundProperty, value);
        }

        public static readonly DependencyProperty ForegroundKeyProperty =
            DependencyProperty.Register(nameof(ForegroundKey), typeof(string), typeof(CustomTextBlock),
                new PropertyMetadata("GrayColor1", OnForegroundKeyChanged));

        private static void OnForegroundKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (CustomTextBlock)d;
            if (e.NewValue != null)
            {
                element.ForegroundKey = (string)e.NewValue;
            }
        }

        public string ForegroundKey
        {
            get => (string)GetValue(ForegroundKeyProperty);
            set
            {
                SetValue(ForegroundKeyProperty, value);
                var brush = MainWindow.Instance?.FindResource(value) as Brush;
                SetResourceReference(ForegroundProperty, brush);
                // if (Application.Current.Resources.Contains(value))
                // {
                //     SetResourceReference(ForegroundProperty, value);
                //     return;
                // }
            }
        }
    }
}
