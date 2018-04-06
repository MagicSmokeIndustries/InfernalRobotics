cd /D "%~dp0"

md "_release"
md "_release\GameData142"
md "_release\GameData142\GameData"
md "_release\GameData142\GameData\MagicSmokeIndustries"

xcopy "Resources\GameData\MagicSmokeIndustries\Agencies" "_release\GameData142\GameData\MagicSmokeIndustries\Agencies\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\AssetBundles" "_release\GameData142\GameData\MagicSmokeIndustries\AssetBundles\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\Flags" "_release\GameData142\GameData\MagicSmokeIndustries\Flags\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\Plugins" "_release\GameData142\GameData\MagicSmokeIndustries\Plugins\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\Sounds" "_release\GameData142\GameData\MagicSmokeIndustries\Sounds\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\SupportedLicenses" "_release\GameData142\GameData\MagicSmokeIndustries\SupportedLicenses\" /S /E /Y

copy "Resources\GameData\MagicSmokeIndustries\InfernalRobotics_1.4.version" "_release\GameData142\GameData\MagicSmokeIndustries\InfernalRobotics.version"

xcopy "Resources\IR-LegacyParts\GameData" "_release\GameData142\GameData\" /S /E /Y

xcopy "Resources\IR-ReworkParts\GameData" "_release\GameData142\GameData\" /S /E /Y

copy "InfernalRobotics\InfernalRobotics\bin\Release 1.4.2\InfernalRobotics_v3.dll" "_release\GameData142\GameData\MagicSmokeIndustries\Plugins\InfernalRobotics_v3.dll"

copy "InfernalRobotics\InfernalRobotics\bin\Release 1.4.2\Scale_Redist.dll" "_release\GameData142\GameData\MagicSmokeIndustries\Plugins\Scale_Redist.dll"

del "_release\InfernalRobotics_v3.0.0_for_1.4.2.zip"
C:\PACL\PACOMP.EXE -a -r -p "_release\InfernalRobotics_v3.0.0_for_1.4.2.zip" "_release\GameData142\*"
