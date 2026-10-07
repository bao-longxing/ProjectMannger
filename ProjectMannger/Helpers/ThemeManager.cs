using System;
using System.Windows;
using System.Windows.Media;

namespace ProjectMannger.Helpers
{
    /// <summary>
    /// 全局主题管理器 —— 管理浅色/深色主题切换
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// 主题变化后触发，UI 层订阅以更新颜色
        /// </summary>
        public static event Action ThemeChanged;

        public static bool IsDarkMode { get; private set; } = false;

        public static void SetDarkMode(bool dark)
        {
            if (IsDarkMode == dark) return;
            IsDarkMode = dark;

            // 切换 ResourceDictionary，使 XAML 中的 DynamicResource 自动更新
            SwitchResourceDictionary(dark);

            ThemeChanged?.Invoke();
        }

        public static void Toggle()
        {
            SetDarkMode(!IsDarkMode);
        }

        /// <summary>
        /// 在运行时切换 Application 级别的主题 ResourceDictionary
        /// </summary>
        private static void SwitchResourceDictionary(bool dark)
        {
            var app = Application.Current;
            if (app == null) return;

            var mergedDicts = app.Resources.MergedDictionaries;
            string themeUri = dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

            // 移除旧的主题 ResourceDictionary
            for (int i = mergedDicts.Count - 1; i >= 0; i--)
            {
                var source = mergedDicts[i].Source;
                if (source != null && (source.OriginalString.Contains("LightTheme") || source.OriginalString.Contains("DarkTheme")))
                {
                    mergedDicts.RemoveAt(i);
                }
            }

            // 加载新的主题 ResourceDictionary
            mergedDicts.Add(new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            });
        }

        // ==================== 语义颜色定义 ====================

        // 窗口 / 外壳
        public static Color WindowBg => IsDarkMode ? Color.FromRgb(17, 24, 39) : Color.FromRgb(244, 246, 249);
        public static Color WindowBorder => IsDarkMode ? Color.FromRgb(55, 65, 81) : Color.FromRgb(226, 232, 240);

        // 左侧导航栏
        public static Color NavBg => IsDarkMode ? Color.FromRgb(30, 41, 59) : Colors.White;
        public static Color NavBorder => IsDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(238, 242, 245);

        // 内容区域
        public static Color ContentBg => IsDarkMode ? Color.FromRgb(17, 24, 39) : Colors.Transparent;

        // 右侧详情面板
        public static Color DetailBg => IsDarkMode ? Color.FromRgb(30, 41, 59) : Colors.White;
        public static Color DetailBorder => IsDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(238, 242, 245);

        // 卡片 / 区块
        public static Color CardBg => IsDarkMode ? Color.FromRgb(30, 41, 59) : Colors.White;
        public static Color CardBorder => IsDarkMode ? Color.FromRgb(55, 65, 81) : Color.FromRgb(226, 232, 240);
        public static Color SectionBg => IsDarkMode ? Color.FromRgb(30, 41, 59) : Color.FromRgb(248, 250, 252);
        public static Color HoverBg => IsDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(237, 245, 255);

        // 文字
        public static Color TextPrimary => IsDarkMode ? Color.FromRgb(241, 245, 249) : Color.FromRgb(15, 23, 42);      // #0F172A → #F1F5F9
        public static Color TextSecondary => IsDarkMode ? Color.FromRgb(226, 232, 240) : Color.FromRgb(30, 41, 59);    // #1E293B → #E2E8F0
        public static Color TextTertiary => IsDarkMode ? Color.FromRgb(203, 213, 225) : Color.FromRgb(51, 65, 85);     // #334155 → #CBD5E1
        public static Color TextMuted => IsDarkMode ? Color.FromRgb(148, 163, 184) : Color.FromRgb(100, 116, 139);     // #64748B 不变
        public static Color TextPlaceholder => IsDarkMode ? Color.FromRgb(100, 116, 139) : Color.FromRgb(148, 163, 184);// #94A3B8 → #64748B

        // 分割线 / 边框
        public static Color Divider => IsDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(241, 245, 249);
        public static Color BorderDefault => IsDarkMode ? Color.FromRgb(71, 85, 105) : Color.FromRgb(226, 232, 240);

        // 强调色（蓝色，两个主题共用）
        public static Color Accent => Color.FromRgb(37, 99, 235);           // #2563EB
        public static Color AccentHover => Color.FromRgb(29, 78, 216);      // #1D4ED8
        public static Color AccentPressed => Color.FromRgb(30, 64, 175);    // #1E40AF
        public static Color AccentLightBg => IsDarkMode ? Color.FromRgb(30, 58, 95) : Color.FromRgb(237, 245, 255);

        // 输入框
        public static Color InputBg => IsDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(248, 250, 252);

        // 标题栏窗口控制按钮
        public static Color WinCtrlHover => IsDarkMode ? Color.FromRgb(55, 65, 81) : Color.FromRgb(226, 232, 240);
        public static Color WinCtrlPressed => IsDarkMode ? Color.FromRgb(75, 85, 99) : Color.FromRgb(203, 213, 225);
        public static Color CloseBtnHover => Color.FromRgb(239, 68, 68);    // 共用
        public static Color CloseBtnPressed => Color.FromRgb(220, 38, 38);  // 共用

        // 进度条
        public static Color ProgressTrack => IsDarkMode ? Color.FromRgb(55, 65, 81) : Color.FromRgb(226, 232, 240);
        public static Color ProgressFill => Accent; // 共用蓝色

        // 统计卡片描边
        public static Color StatCardStroke => IsDarkMode ? Color.FromRgb(55, 65, 81) : Color.FromRgb(226, 232, 240);

        // Drop Shadow
        public static Color ShadowColor => IsDarkMode ? Colors.Black : Color.FromRgb(0, 0, 0);

        // 优先级（保持鲜艳，两个主题共用）
        public static Color PriorityHighBg => Color.FromArgb(255, 254, 242, 242);
        public static Color PriorityHighFg => Color.FromRgb(239, 68, 68);
        public static Color PriorityMedBg => Color.FromArgb(255, 255, 251, 235);
        public static Color PriorityMedFg => Color.FromRgb(217, 119, 6);
        public static Color PriorityLowBg => Color.FromArgb(255, 239, 246, 255);
        public static Color PriorityLowFg => Color.FromRgb(37, 99, 235);
        public static Color PriorityDefaultBg => Color.FromArgb(255, 241, 245, 249);
        public static Color PriorityDefaultFg => Color.FromRgb(100, 116, 139);

        // 阶段
        public static Color StageDevBg => Color.FromArgb(255, 239, 246, 255);
        public static Color StageDevFg => Color.FromRgb(37, 99, 235);
        public static Color StageTestBg => Color.FromArgb(255, 255, 251, 235);
        public static Color StageTestFg => Color.FromRgb(217, 119, 6);
        public static Color StageDoneBg => Color.FromArgb(255, 236, 253, 245);
        public static Color StageDoneFg => Color.FromRgb(16, 185, 129);
        public static Color StageDefaultBg => Color.FromArgb(255, 241, 245, 249);
        public static Color StageDefaultFg => Color.FromRgb(100, 116, 139);

        // ==================== 工具方法 ====================

        public static SolidColorBrush Brush(Color c) => new SolidColorBrush(c);
        public static SolidColorBrush Brush(System.Windows.Media.Color c, byte alpha)
            => new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
    }
}
