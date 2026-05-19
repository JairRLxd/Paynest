using UIKit;

namespace Paynest;

// Called from AppShell.xaml.cs via the Navigated event after MAUI has fully
// set up the UITabBarController and applied its own ShellTabBarAppearanceTracker.
// We override on the INSTANCE (not the global Appearance proxy) so our settings
// win over whatever MAUI's tracker wrote.  IconColor must be set explicitly when
// StandardAppearance is active (iOS 15+), otherwise TintColor is ignored.
public partial class AppShell
{
    private bool _tabBarConfigured;

    partial void PlatformOnNavigated()
    {
        if (_tabBarConfigured) return;

        var tabBar = FindTabBar();
        if (tabBar == null) return;

        _tabBarConfigured = true;
        ApplyTabBarColors(tabBar);
    }

    private static void ApplyTabBarColors(UITabBar tabBar)
    {
        var inactive = UIColor.FromRGB(0x7A, 0x76, 0x6E); // #7A766E
        var active   = UIColor.FromRGB(0x2A, 0x63, 0x49); // #2A6349
        var bgColor  = UIColor.FromRGB(0xF2, 0xF0, 0xEB); // #F2F0EB

        // Direct tint (fallback for non-StandardAppearance path).
        tabBar.TintColor               = active;
        tabBar.UnselectedItemTintColor = inactive;

        // UITabBarAppearance path (iOS 15+): IconColor MUST be set explicitly
        // because when StandardAppearance is active, TintColor is overridden.
        var itemAppearance = new UITabBarItemAppearance();
        itemAppearance.Normal.IconColor             = inactive;
        itemAppearance.Normal.TitleTextAttributes   = new UIStringAttributes { ForegroundColor = inactive };
        itemAppearance.Selected.IconColor           = active;
        itemAppearance.Selected.TitleTextAttributes = new UIStringAttributes { ForegroundColor = active };

        var appearance = new UITabBarAppearance();
        appearance.ConfigureWithOpaqueBackground();
        appearance.BackgroundColor           = bgColor;
        appearance.StackedLayoutAppearance   = itemAppearance;
        appearance.InlineLayoutAppearance    = itemAppearance;
        appearance.CompactInlineLayoutAppearance = itemAppearance;

        tabBar.StandardAppearance = appearance;
        if (OperatingSystem.IsIOSVersionAtLeast(15))
            tabBar.ScrollEdgeAppearance = appearance;
    }

    private static UITabBar? FindTabBar()
    {
        var window = UIApplication.SharedApplication
            .ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        return FindTabBarInHierarchy(window?.RootViewController);
    }

    private static UITabBar? FindTabBarInHierarchy(UIViewController? vc)
    {
        if (vc is UITabBarController tbc) return tbc.TabBar;

        foreach (var child in vc?.ChildViewControllers ?? [])
        {
            var result = FindTabBarInHierarchy(child);
            if (result != null) return result;
        }

        return vc?.PresentedViewController is { } presented
            ? FindTabBarInHierarchy(presented)
            : null;
    }
}
