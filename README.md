# PackageChecker
A simple script for checking Unity package dependencies in projects. Useful for asset store publishing.

## Install
Add the contents directly to your project's Assets folder.

## How it works
PackageChecker will run each time the project is loaded or the PackageChecker.cs script is modified. The script creates a **PackageCheckerInfo.asset** file in the editor subfolder that uses a simple version number to make sure that packages are only checked once.

If you want to force a check, you can access the package checker from the Unity editor toolbar at **Tools/PackageChecker/Check Packages**. This will ignore the version number.

To use in your own project, you need to do add the relevant package names to the **k_Packages** constant. Every time you change this array you should also increment the **k_TargetVersion** constant so that the checker realizes it is out of date on the next run.

You can also integrate better into your own project by moving the files to another folder (They must be stored in an Editor folder), and updating the **k_MenuEntry** and **k_DefaultPackageCheckerPath** constants to better match your needs.
