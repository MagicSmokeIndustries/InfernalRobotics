cd /D "%~dp0"

md "_release"
md "_release\GameData122"
md "_release\GameData122\GameData"
md "_release\GameData122\GameData\MagicSmokeIndustries"

xcopy "Resources\GameData\MagicSmokeIndustries\Agencies" "_release\GameData122\GameData\MagicSmokeIndustries\Agencies\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\AssetBundles" "_release\GameData122\GameData\MagicSmokeIndustries\AssetBundles\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\Flags" "_release\GameData122\GameData\MagicSmokeIndustries\Flags\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\Plugins" "_release\GameData122\GameData\MagicSmokeIndustries\Plugins\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\Sounds" "_release\GameData122\GameData\MagicSmokeIndustries\Sounds\" /S /E /Y
xcopy "Resources\GameData\MagicSmokeIndustries\SupportedLicenses" "_release\GameData122\GameData\MagicSmokeIndustries\SupportedLicenses\" /S /E /Y

copy "GameData\KerbalJointReinforcement\InfernalRobotics_1.2.2.version" "_release\GameData122\GameData\MagicSmokeIndustries\InfernalRobotics.version"

xcopy "Resources\IR-LegacyParts\GameData" "_release\GameData122\GameData\" /S /E /Y

xcopy "Resources\IR-ReworkParts\GameData" "_release\GameData122\GameData\" /S /E /Y

copy "InfernalRobotics\InfernalRobotics\bin\Release 1.2.2\InfernalRobotics_v3.dll" "_release\GameData122\GameData\MagicSmokeIndustries\Plugins\InfernalRobotics_v3.dll"

copy "InfernalRobotics\InfernalRobotics\bin\Release 1.2.2\KerbalJointReinforcement_Redist.dll" "_release\GameData122\GameData\MagicSmokeIndustries\Plugins\KerbalJointReinforcement_Redist.dll"

copy "InfernalRobotics\InfernalRobotics\bin\Release 1.2.2\Scale_Redist.dll" "_release\GameData122\GameData\MagicSmokeIndustries\Plugins\Scale_Redist.dll"

C:\PACL\PACOMP.EXE -a -r -p "_release\InfernalRobotics_v3.0.0_for_1.2.2.zip" "_release\GameData122\*"
