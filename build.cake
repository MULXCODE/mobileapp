#tool "nuget:?package=xunit.runner.console&version=2.2.0"
#tool "nuget:?package=NUnit.Runners&version=2.6.3"

public class TemporaryFileTransformation
{
    public string Path { get; set; }
    public string Original { get; set; }
    public string Temporary { get; set; }
}

var target = Argument("target", "Default");
var buildAll = Argument("buildall", Bitrise.IsRunningOnBitrise);

private void FormatAndroidAxml()
{
	var args = "tools/xml-format/WilliamizeXml.Console.dll Toggl.Droid/Resources/layout/";

	StartProcess("mono", new ProcessSettings { Arguments = args });
}

private void GenerateSyncDiagram()
{
    var args = "bin/Debug/netcoreapp2.0/SyncDiagramGenerator.dll";

    StartProcess("dotnet", new ProcessSettings { Arguments = args });
}

private Action Test(string[] projectPaths)
{
    var settings = new DotNetCoreTestSettings { NoBuild = true };

    return () =>
    {
        foreach (var projectPath in projectPaths)
        {
            DotNetCoreTest(projectPath, settings);
        }
    };
}

private Action BuildSolution(string configuration, string platform = "")
{
    const string togglSolution = "./Toggl.sln";
    var buildSettings = new MSBuildSettings
    {
        Verbosity = Bitrise.IsRunningOnBitrise ? Verbosity.Verbose : Verbosity.Minimal,
        Configuration = configuration
    };

    if (!string.IsNullOrEmpty(platform))
    {
        buildSettings = buildSettings.WithProperty("Platform", platform);
    }

	return () => MSBuild(togglSolution, buildSettings);
}

private Action GenerateApk(string configuration)
{
    const string droidProject = "./Toggl.Droid/Toggl.Droid.csproj";
    var buildSettings = new MSBuildSettings
    {
        Verbosity = Bitrise.IsRunningOnBitrise ? Verbosity.Verbose : Verbosity.Minimal,
        Configuration = configuration
    };

    buildSettings.WithTarget("SignAndroidPackage");

    return () => MSBuild(droidProject, buildSettings);
}

private string GetCommitHash()
{
    IEnumerable<string> redirectedOutput;
    StartProcess("git", new ProcessSettings
    {
        Arguments = "rev-parse HEAD",
        RedirectStandardOutput = true
    }, out redirectedOutput);

    return redirectedOutput.Last();
}

private string GetCommitCount()
{
    IEnumerable<string> redirectedOutput;
    StartProcess("git", new ProcessSettings
    {
        Arguments = "rev-list --count HEAD",
        RedirectStandardOutput = true
    }, out redirectedOutput);

    return redirectedOutput.Last();
}

private TemporaryFileTransformation GetAndroidProjectConfigurationTransformation()
{
    const string path = "Toggl.Droid/Toggl.Droid.csproj";
    var storePass = EnvironmentVariable("BITRISEIO_ANDROID_KEYSTORE_PASSWORD");
    var keyAlias = EnvironmentVariable("BITRISEIO_ANDROID_KEYSTORE_ALIAS");
    var keyPass = EnvironmentVariable("BITRISEIO_ANDROID_KEYSTORE_PRIVATE_KEY_PASSWORD");

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{KEYSTORE_PASSWORD}", storePass)
                        .Replace("{KEYSTORE_ALIAS}", keyAlias)
                        .Replace("{KEYSTORE_ALIAS_PASSWORD}", keyPass)
    };
}

private TemporaryFileTransformation GetIosAnalyticsServicesConfigurationTransformation()
{
    const string path = "Toggl.iOS/GoogleService-Info.plist";
    var adUnitForBannerTest = EnvironmentVariable("TOGGL_AD_UNIT_ID_FOR_BANNER_TEST");
    var adUnitIdForInterstitialTest = EnvironmentVariable("TOGGL_AD_UNIT_ID_FOR_INTERSTITIAL_TEST");
    var clientId = EnvironmentVariable("TOGGL_CLIENT_ID");
    var reversedClientId = EnvironmentVariable("TOGGL_REVERSED_CLIENT_ID");
    var apiKey = EnvironmentVariable("TOGGL_API_KEY");
    var gcmSenderId = EnvironmentVariable("TOGGL_GCM_SENDER_ID");
    var projectId = EnvironmentVariable("TOGGL_PROJECT_ID");
    var storageBucket = EnvironmentVariable("TOGGL_STORAGE_BUCKET");
    var googleAppId = EnvironmentVariable("TOGGL_GOOGLE_APP_ID");

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{TOGGL_AD_UNIT_ID_FOR_BANNER_TEST}", adUnitForBannerTest)
                        .Replace("{TOGGL_AD_UNIT_ID_FOR_INTERSTITIAL_TEST}", adUnitIdForInterstitialTest)
                        .Replace("{TOGGL_CLIENT_ID}", clientId)
                        .Replace("{TOGGL_REVERSED_CLIENT_ID}", reversedClientId)
                        .Replace("{TOGGL_API_KEY}", apiKey)
                        .Replace("{TOGGL_GCM_SENDER_ID}", gcmSenderId)
                        .Replace("{TOGGL_PROJECT_ID}", projectId)
                        .Replace("{TOGGL_STORAGE_BUCKET}", storageBucket)
                        .Replace("{TOGGL_GOOGLE_APP_ID}", googleAppId)
    };
}

private TemporaryFileTransformation GetIosAppDelegateTransformation()
{
    const string path = "Toggl.iOS/Startup/AppDelegate.Analytics.cs";
    var adjustToken = EnvironmentVariable("TOGGL_ADJUST_APP_TOKEN");
    string appCenterId = "";

    if (target == "Build.Release.iOS.AdHoc")
    {
        appCenterId = EnvironmentVariable("TOGGL_APP_CENTER_ID_IOS_ADHOC");
    }
    else if (target == "Build.Release.iOS.AppStore")
    {
        appCenterId = EnvironmentVariable("TOGGL_APP_CENTER_ID_IOS");
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{TOGGL_APP_CENTER_ID_IOS}", appCenterId)
                        .Replace("{TOGGL_ADJUST_APP_TOKEN}", adjustToken)
    };
}

private TemporaryFileTransformation GetAndroidGoogleServicesTransformation()
{
    const string path = "Toggl.Droid/google-services.json";
    var gcmSenderId = EnvironmentVariable("TOGGL_GCM_SENDER_ID");
    var databaseUrl = EnvironmentVariable("TOGGL_DATABASE_URL");
    var projectId = EnvironmentVariable("TOGGL_PROJECT_ID");
    var storageBucket = EnvironmentVariable("TOGGL_STORAGE_BUCKET");
    var mobileSdkAppId = EnvironmentVariable("TOGGL_DROID_GOOGLE_SERVICES_MOBILE_SDK_APP_ID");
    var mobileSdkAdhocAppId = EnvironmentVariable("TOGGL_DROID_ADHOC_GOOGLE_SERVICES_MOBILE_SDK_APP_ID");
    var clientId = EnvironmentVariable("TOGGL_DROID_GOOGLE_SERVICES_CLIENT_ID");
    var apiKey = EnvironmentVariable("TOGGL_DROID_GOOGLE_SERVICES_API_KEY");

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{TOGGL_GCM_SENDER_ID}", gcmSenderId)
                        .Replace("{TOGGL_DATABASE_URL}", databaseUrl)
                        .Replace("{TOGGL_PROJECT_ID}", projectId)
                        .Replace("{TOGGL_STORAGE_BUCKET}", storageBucket)
                        .Replace("{TOGGL_DROID_GOOGLE_SERVICES_MOBILE_SDK_APP_ID}", mobileSdkAppId)
                        .Replace("{TOGGL_DROID_ADHOC_GOOGLE_SERVICES_MOBILE_SDK_APP_ID}", mobileSdkAdhocAppId)
                        .Replace("{TOGGL_DROID_GOOGLE_SERVICES_CLIENT_ID}", clientId)
                        .Replace("{TOGGL_DROID_GOOGLE_SERVICES_API_KEY}", apiKey)
    };
}

private TemporaryFileTransformation GetAndroidGoogleLoginTransformation()
{
    const string path = "Toggl.Droid/Activities/ReactiveActivity.GoogleTokenProvider.cs";
    var clientId = EnvironmentVariable("TOGGL_DROID_GOOGLE_SERVICES_CLIENT_ID");

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{TOGGL_DROID_GOOGLE_SERVICES_CLIENT_ID}", clientId)
    };
}

private TemporaryFileTransformation GetIosInfoConfigurationTransformation()
{
    const string path = "Toggl.iOS/Info.plist";
    const string bundleIdToReplace = "com.toggl.daneel.debug";
    const string appNameToReplace = "Toggl for Devs";
    const string iconSetToReplace = "Assets.xcassets/AppIcon-debug.appiconset";

    var commitCount = GetCommitCount();
    var reversedClientId = EnvironmentVariable("TOGGL_REVERSED_CLIENT_ID");

    var bundleId = bundleIdToReplace;
    var appName = appNameToReplace;
    var iconSet = iconSetToReplace;

    if (target == "Build.Release.iOS.AdHoc")
    {
        bundleId = "com.toggl.daneel.adhoc";
        appName = "Toggl for Tests";
        iconSet = "Assets.xcassets/AppIcon-adhoc.appiconset";
    }
    else if (target == "Build.Release.iOS.AppStore")
    {
        bundleId = "com.toggl.daneel";
        appName = "Toggl";
        iconSet = "Assets.xcassets/AppIcon.appiconset";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{TOGGL_REVERSED_CLIENT_ID}", reversedClientId)
                        .Replace("IOS_BUNDLE_VERSION", commitCount)
                        .Replace(bundleIdToReplace, bundleId)
                        .Replace(appNameToReplace, appName)
                        .Replace(iconSetToReplace, iconSet)
    };
}

private TemporaryFileTransformation GetIosSiriExtensionInfoConfigurationTransformation()
{
    const string path = "Toggl.iOS.SiriExtension/Info.plist";
    const string bundleIdToReplace = "com.toggl.daneel.debug.SiriExtension";
    const string appNameToReplace = "Siri Extension Development";

    var commitCount = GetCommitCount();

    var bundleId = bundleIdToReplace;
    var appName = appNameToReplace;

    if (target == "Build.Release.iOS.AdHoc")
    {
        bundleId = "com.toggl.daneel.adhoc.SiriExtension";
        appName = "Siri Extension Development";
    }
    else if (target == "Build.Release.iOS.AppStore")
    {
        bundleId = "com.toggl.daneel.SiriExtension";
        appName = "Siri Extension";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("IOS_BUNDLE_VERSION", commitCount)
                        .Replace(bundleIdToReplace, bundleId)
                        .Replace(appNameToReplace, appName)
    };
}

private TemporaryFileTransformation GetIosSiriUIExtensionInfoConfigurationTransformation()
{
    const string path = "Toggl.iOS.SiriExtension.UI/Info.plist";
    const string bundleIdToReplace = "com.toggl.daneel.debug.SiriUIExtension";
    const string appNameToReplace = "Toggl.Daneel.SiriExtension.UI";

    var commitCount = GetCommitCount();

    var bundleId = bundleIdToReplace;
    var appName = appNameToReplace;

    if (target == "Build.Release.iOS.AdHoc")
    {
        bundleId = "com.toggl.daneel.adhoc.SiriUIExtension";
        appName = "Siri UI Extension Development";
    }
    else if (target == "Build.Release.iOS.AppStore")
    {
        bundleId = "com.toggl.daneel.SiriUIExtension";
        appName = "Siri UI Extension";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("IOS_BUNDLE_VERSION", commitCount)
                        .Replace(bundleIdToReplace, bundleId)
                        .Replace(appNameToReplace, appName)
    };
}

private TemporaryFileTransformation GetIosEntitlementsConfigurationTransformation()
{
    const string path = "Toggl.iOS/Entitlements.plist";
    const string groupIdToReplace = "group.com.toggl.daneel.debug.extensions";
    const string defaultApsEnvironment = "<string>development</string>";

    var groupId = groupIdToReplace;
    var apsEnvironment = defaultApsEnvironment;

    if (target == "Build.Release.iOS.AdHoc")
    {
        groupId = "group.com.toggl.daneel.adhoc.extensions";
        apsEnvironment = "<string>production</string>";
    }
    else if (target == "Build.Release.iOS.AppStore")
    {
        groupId = "group.com.toggl.daneel.extensions";
        apsEnvironment = "<string>production</string>";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace(groupIdToReplace, groupId).Replace(defaultApsEnvironment, apsEnvironment)
    };
}

private TemporaryFileTransformation GetIosExtensionEntitlementsConfigurationTransformation()
{
    const string path = "Toggl.iOS.SiriExtension/Entitlements.plist";
    const string groupIdToReplace = "group.com.toggl.daneel.debug.extensions";

    var groupId = groupIdToReplace;

    if (target == "Build.Release.iOS.AdHoc")
    {
        groupId = "group.com.toggl.daneel.adhoc.extensions";
    }
    else if (target == "Build.Release.iOS.AppStore")
    {
        groupId = "group.com.toggl.daneel.extensions";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace(groupIdToReplace, groupId)
    };
}

private TemporaryFileTransformation GetAndroidManifestTransformation()
{
    const string path = "Toggl.Droid/Properties/AndroidManifest.xml";
    const string packageNameToReplace = "com.toggl.giskard.debug";
    const string versionNumberToReplace = "987654321";
    const string appNameToReplace = "Toggl for Devs";

    var commitCount = GetCommitCount();
    var packageName = packageNameToReplace;
    var appName = appNameToReplace;

    if (target == "Build.Release.Android.AdHoc")
    {
        packageName = "com.toggl.giskard.adhoc";
        appName = "Toggl for Tests";
    }
    else if (target == "Build.Release.Android.PlayStore")
    {
        packageName = "com.toggl.giskard";
        appName = "Toggl";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace(versionNumberToReplace, commitCount)
                        .Replace(packageNameToReplace, packageName)
                        .Replace(appNameToReplace, appName)
    };
}

private TemporaryFileTransformation GetAndroidSplashScreenTransformation()
{
    const string path = "Toggl.Droid/Startup/SplashScreen.cs";
    const string appNameToReplace = "Toggl for Devs";

    var appName = appNameToReplace;

    if (target == "Build.Release.Android.AdHoc")
    {
        appName = "Toggl for Tests";
    }
    else if (target == "Build.Release.Android.PlayStore")
    {
        appName = "Toggl";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace(appNameToReplace, appName)
    };
}

private TemporaryFileTransformation GetAndroidTogglApplicationTransformation()
{
    const string path = "Toggl.Droid/Startup/TogglApplication.cs";
    string appCenterId = "";

    if (target == "Build.Release.Android.AdHoc")
    {
        appCenterId = EnvironmentVariable("TOGGL_APP_CENTER_ID_DROID_ADHOC");
    }
    else if (target == "Build.Release.Android.PlayStore")
    {
        appCenterId = EnvironmentVariable("TOGGL_APP_CENTER_ID_DROID");
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("{TOGGL_APP_CENTER_ID_DROID}", appCenterId)
    };
}

private TemporaryFileTransformation GetIntegrationTestsConfigurationTransformation()
{
    const string path = "Toggl.Networking.Tests.Integration/Helper/Configuration.cs";
    var commitHash = GetCommitHash();
    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace("\"CAKE_COMMIT_HASH\"", $"\"{commitHash}\"")
    };
}

private TemporaryFileTransformation GetAndroidAppIconTransformation()
{
    const string path = "Toggl.Droid/Resources/mipmap-anydpi-v26/ic_launcher.xml";
    const string drawableToReplace = "@color/launcherBackgroundDebug";
    var drawable = "@color/launcherBackgroundDebug";
    
    if (target == "Build.Release.Android.AdHoc")
    {
        drawable = "@color/launcherBackgroundAdHoc";
    }
    else if (target == "Build.Release.Android.PlayStore")
    {
        drawable = "@color/launcherBackground";
    }

    var filePath = GetFiles(path).Single();
    var file = TransformTextFile(filePath).ToString();

    return new TemporaryFileTransformation
    {
        Path = path,
        Original = file,
        Temporary = file.Replace(drawableToReplace, drawable)
    };
}

var transformations = new List<TemporaryFileTransformation>
{
    GetIosInfoConfigurationTransformation(),
    GetIosSiriExtensionInfoConfigurationTransformation(),
    GetIosSiriUIExtensionInfoConfigurationTransformation(),
    GetIosAppDelegateTransformation(),
    GetIntegrationTestsConfigurationTransformation(),
    GetIosAnalyticsServicesConfigurationTransformation(),
    GetIosEntitlementsConfigurationTransformation(),
    GetIosExtensionEntitlementsConfigurationTransformation(),
    GetAndroidProjectConfigurationTransformation(),
    GetAndroidGoogleServicesTransformation(),
    GetAndroidGoogleLoginTransformation(),
    GetAndroidSplashScreenTransformation(),
    GetAndroidTogglApplicationTransformation(),
    GetAndroidManifestTransformation(),
    GetAndroidAppIconTransformation(),
};

private HashSet<string> targetsThatSkipTearDown = new HashSet<string>
{
    "Build.Release.iOS.AdHoc",
    "Build.Release.iOS.AppStore",
    "Build.Release.Android.AdHoc",
    "Build.Release.Android.PlayStore"
};

private string[] GetUnitTestProjects() => new []
{
    "./Toggl.Shared.Tests/Toggl.Shared.Tests.csproj",
    "./Toggl.Networking.Tests/Toggl.Networking.Tests.csproj",
    "./Toggl.Storage.Tests/Toggl.Storage.Tests.csproj",
    "./Toggl.Core.Tests/Toggl.Core.Tests.csproj",
};

private string[] GetIntegrationTestProjects()
    => new [] { "./Toggl.Networking.Tests.Integration/Toggl.Networking.Tests.Integration.csproj" };

private string[] GetSyncTestProjects()
    => new [] { "./Toggl.Core.Tests.Sync/Toggl.Core.Tests.Sync.csproj" };

Setup(context => transformations.ForEach(transformation => System.IO.File.WriteAllText(transformation.Path, transformation.Temporary)));
Teardown(context =>
{
    if (targetsThatSkipTearDown.Contains(target))
        return;

    transformations.ForEach(transformation => System.IO.File.WriteAllText(transformation.Path, transformation.Original));
});

//Build
Task("Clean")
    .Does(() =>
        {
            CleanDirectory("./bin");
            CleanDirectory("./Toggl.iOS/obj");
            CleanDirectory("./Toggl.iOS.SiriExtension/obj");
            CleanDirectory("./Toggl.iOS.SiriExtension.UI/obj");
            CleanDirectory("./Toggl.iOS.Tests/obj");
            CleanDirectory("./Toggl.iOS.Tests.UI/obj");
            CleanDirectory("./Toggl.Droid/obj");
            CleanDirectory("./Toggl.Droid.Tests/obj");
            CleanDirectory("./Toggl.Droid.Tests.UI/obj");
            CleanDirectory("./Toggl.Core/obj");
            CleanDirectory("./Toggl.Core.UI/obj");
            CleanDirectory("./Toggl.Core.Tests/obj");
            CleanDirectory("./Toggl.Shared/obj");
            CleanDirectory("./Toggl.Shared.Tests/obj");
            CleanDirectory("./Toggl.Storage/obj");
            CleanDirectory("./Toggl.Storage.Realm/obj");
            CleanDirectory("./Toggl.Storage.Tests/obj");
            CleanDirectory("./Toggl.Networking/obj");
            CleanDirectory("./Toggl.Networking.Tests/obj");
            CleanDirectory("./Toggl.Networking.Tests.Integration/obj");
            CleanDirectory("./Toggl.Tools/SyncDiagramGenerator/obj");
        });

Task("Format")
    .IsDependentOn("Clean")
    .Does(() => FormatAndroidAxml());

Task("Nuget")
    .IsDependentOn("Clean")
    .Does(() => NuGetRestore("./Toggl.sln"));

Task("Build.Tests.All")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("Debug"))
    .Does(GenerateApk("Debug"));

Task("Build.Tests.Unit")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("UnitTests"));

Task("Build.Tests.Integration")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("ApiTests"));

Task("Build.Tests.Sync")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("SyncTests"));

Task("BuildSyncDiagramGenerator")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("SyncDiagramGenerator"));

Task("GenerateSyncDiagram")
    .Does(() => GenerateSyncDiagram());

//iOS Builds
Task("Build.Debug.iOS")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("Debug.iOS.Fast", "iPhoneSimulator"));

//iOS Builds
Task("Build.Release.iOS.AdHoc")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("Release.AdHoc"));

Task("Build.Release.iOS.AppStore")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("Release.AppStore", ""));

//Android Builds
Task("Build.Release.Android.AdHoc")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("Release.AdHoc.Giskard", ""));

Task("Build.Release.Android.PlayStore")
    .IsDependentOn("Nuget")
    .Does(BuildSolution("Release.PlayStore", ""));

//Unit Tests
Task("Tests.Unit")
    .IsDependentOn(buildAll ? "Build.Tests.All" : "Build.Tests.Unit")
    .Does(Test(GetUnitTestProjects()));

//Integration Tests
Task("Tests.Integration")
    .IsDependentOn("Build.Tests.Integration")
    .Does(Test(GetIntegrationTestProjects()));

//Integration Tests
Task("Tests.Sync")
    .IsDependentOn("Build.Tests.Sync")
    .Does(Test(GetSyncTestProjects()));

// All Tests
Task("Tests")
    .IsDependentOn("Tests.Unit")
    .IsDependentOn("Tests.Integration");

//Default Operation
Task("Default")
    .IsDependentOn("Tests.Unit");

RunTarget(target);
