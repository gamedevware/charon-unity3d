/*
	Copyright (c) 2023 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

// ReSharper disable All

namespace GameDevWare.Charon {
	internal static class Resources
	{
		public const string UI_UNITYPLUGIN_ERROR_SCRIPTS_COMPILING = "Interrupted by Unity's script compilation. Please retry after Unity has finished script compilation.";
		public const string UI_UNITYPLUGIN_ERROR_CANCELLED = "Operation has been cancelled.";
		public const string UI_UNITYPLUGIN_ABOUT_CHARON_TITLE = "Charon: Game Data Editor";
		public const string UI_UNITYPLUGIN_ABOUT_CLOSE_BUTTON = "Close";
		public const string UI_UNITYPLUGIN_ABOUT_EDITOR_PORT = "Editor TCP Port";
		public const string UI_UNITYPLUGIN_ABOUT_EDITOR_VERSION_LABEL = "Editor Version";
		public const string UI_UNITYPLUGIN_COMPILING_WARNING = "No actions can be taken while the Unity Editor is compiling scripts.";
		public const string UI_UNITYPLUGIN_COROUTINE_IS_RUNNIG_WARNING = "No actions can be performed while another action is running.";
		public const string UI_UNITYPLUGIN_DOWNLOAD_BUTTON = "Update (~{0:F1} MiB)";
		public const string UI_UNITYPLUGIN_GENERATE_ASSET_CANT_FIND_GAMEDATA_CLASS = "Asset generation failed: unable to find the game's data type in C# assemblies. Please ensure there are no compilation errors.";
		public const string UI_UNITYPLUGIN_GENERATE_CODE_FOR = "Running source code generator for {0}";
		public const string UI_UNITYPLUGIN_GENERATE_FAILED_DUE_ERRORS = "Source code generation for '{0}' has failed due to errors: {1}.";
		public const string UI_UNITYPLUGIN_GENERATE_REFORMAT_CODE = "Re-formatting generated source code for {0}";
		public const string UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS = "Refreshing assets";
		public const string UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS = "Generating Code and Assets...";
		public const string UI_UNITYPLUGIN_INSPECTOR_ACTIONS_GROUP = "Actions";
		public const string UI_UNITYPLUGIN_INSPECTOR_ADD_ASSET_BUTTON = "Add Asset";
		public const string UI_UNITYPLUGIN_INSPECTOR_ADD_BUTTON = "Add";
		public const string UI_UNITYPLUGIN_INSPECTOR_ADD_NAME_BUTTON = "Add Name";
		public const string UI_UNITYPLUGIN_INSPECTOR_ASSET_GENERATION_PATH = "Asset Generation Path";
		public const string UI_UNITYPLUGIN_INSPECTOR_ASSET_LABEL = "Asset";
		public const string UI_UNITYPLUGIN_INSPECTOR_AUTO_GENERATION = "Auto-Generation";
		public const string UI_UNITYPLUGIN_INSPECTOR_SPLIT_FILES = "Split into multple files";
		public const string UI_UNITYPLUGIN_INSPECTOR_AUTO_SYNC = "Auto-Synchronize";
		public const string UI_UNITYPLUGIN_INSPECTOR_PROJECT_LABEL = "Project";
		public const string UI_UNITYPLUGIN_INSPECTOR_BRANCH_LABEL = "Branch";
		public const string UI_UNITYPLUGIN_INSPECTOR_LAST_SYNCHRONIZATION_LABEL = "Last Synchronization";
		public const string UI_UNITYPLUGIN_INSPECTOR_BACKUP_BUTTON = "Backup";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GAMEDATA_CLASS_NAME = "Game Data Class";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_DOCUMENT_CLASS_NAME = "Document Class";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_LABEL = "Code Generation";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH = "Generation Path";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATOR = "Code Generator";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_INDENTATION = "Indentation";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_LINE_ENDINGS = "Line Endings";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_NAMESPACE = "Namespace";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_OPTIMIZATIONS = "Optimizations";
		public const string UI_UNITYPLUGIN_INSPECTOR_EDIT_BUTTON = "Edit";
		public const string UI_UNITYPLUGIN_INSPECTOR_GENERATION_PREFIX = "Generation:";
		public const string UI_UNITYPLUGIN_INSPECTOR_NAME_LABEL = "Name";
		public const string UI_UNITYPLUGIN_INSPECTOR_RESTORE_BUTTON = "Restore";
		public const string UI_UNITYPLUGIN_INSPECTOR_RUN_GENERATOR_BUTTON = "Generate Source Code";
		public const string UI_UNITYPLUGIN_INSPECTOR_SYNCHRONIZE_BUTTON = "Synchronize";
		public const string UI_UNITYPLUGIN_INSPECTOR_CONNECT_BUTTON = "Connect";
		public const string UI_UNITYPLUGIN_INSPECTOR_DISCONNECT_BUTTON = "Disconnect";
		public const string UI_UNITYPLUGIN_INSPECTOR_FORMULA_ASSEMBLIES_LABEL = "Assemblies Exposed to Formulas";
		public const string UI_UNITYPLUGIN_INSPECTOR_CONNECTION_LABEL = "Connection";
		public const string UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL = "<Not Connected>";
		public const string UI_UNITYPLUGIN_INSPECTOR_VALIDATE_BUTTON = "Validate";
		public const string UI_UNITYPLUGIN_INSPECTOR_VALIDATION_PREFIX = "Validation:";
		public const string UI_UNITYPLUGIN_INSPECTOR_SYNCHONIZATION_PREFIX = "Synchronization:";
		public const string UI_UNITYPLUGIN_INSPECTOR_LAUNCHING_EDITOR_PREFIX = "Launching Editor:";
		public const string UI_UNITYPLUGIN_MENU_SETTINGS = "Settings...";
		public const string UI_UNITYPLUGIN_MENU_ADVANCED = "Advanced";
		public const string UI_UNITYPLUGIN_MENU_CHECK_RUNTIME = "Check Runtime...";
		public const string UI_UNITYPLUGIN_MENU_CHECK_UPDATES = "Check for Updates...";
		public const string UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA = "Game Data";
		public const string UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON = "Game Data (JSON)";
		public const string UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_MESSAGEPACK = "Game Data (Message Pack)";
		public const string UI_UNITYPLUGIN_MENU_DOCUMENTATION = "Open Documentation";
		public const string UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES = "Extract Code Generation Templates...";
		public const string UI_UNITYPLUGIN_MENU_GENERATE_CODE_AND_ASSETS = "Generate Code and Assets";
		public const string UI_UNITYPLUGIN_MENU_SYNCHRONIZE_ASSETS = "Synchronize Connected Assets";
		public const string UI_UNITYPLUGIN_MENU_OPEN_LOGS = "Open Logs...";
		public const string UI_UNITYPLUGIN_MENU_RESET_PREFERENCES = "Reset Preferences";
		public const string UI_UNITYPLUGIN_MENU_SEND_FEEDBACK = "Report Issue...";
		public const string UI_UNITYPLUGIN_MENU_TROUBLESHOOTING = "Troubleshooting";
		public const string UI_UNITYPLUGIN_MENU_VALIDATE_ASSETS = "Validate Assets";
		public const string UI_UNITYPLUGIN_MENU_VERBOSE_LOGS = "Verbose Logs";
		public const string UI_UNITYPLUGIN_MENU_USE_BETA_FEED = "Use Beta Updates";
		public const string UI_UNITYPLUGIN_OPERATION_CANCELLED = "Operation was cancelled by the user.";
		public const string UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION = "Checking current tools version...";
		public const string UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA = "Processing game data at {0}";
		public const string UI_UNITYPLUGIN_PROGRESS_AUTHENTICATING = "Authenticating";
		public const string UI_UNITYPLUGIN_PROGRESS_DONE = "Done";
		public const string UI_UNITYPLUGIN_PROGRESS_DOWNLOADING = "Downloading '{2}' ({0:F2}/{1:F2}MiB)...";
		public const string UI_UNITYPLUGIN_PROGRESS_GETTING_AVAILABLE_BUILDS = "Getting list of public releases...";
		public const string UI_UNITYPLUGIN_PROGRESS_UNPACKING = "Unpacking '{0}'...";
		public const string UI_UNITYPLUGIN_SELECT_FILE_TO_ATTACH_TITLE = "Select file to attach";
		public const string UI_UNITYPLUGIN_SPECIFY_EXTRACTION_LOC_TITLE = "Specify extraction location...";
		public const string UI_UNITYPLUGIN_T4_EXTRACTION_COMPLETE = "T4 Template has been extracted successfully.";
		public const string UI_UNITYPLUGIN_T4_EXTRACTION_FAILED = "T4 Template extraction failed due to errors: {0}.";
		public const string UI_UNITYPLUGIN_UPDATE_AVAILABLE_MESSAGE = "A new version '{1}' of {2} is available. The current version is '{0}'.";
		public const string UI_UNITYPLUGIN_UPDATE_AVAILABLE_TITLE = "Update Available";
		public const string UI_UNITYPLUGIN_VALIDATE_COMPLETE = "Validation of '{0}' is complete. Result: {1}, errors: {2}.";
		public const string UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS = "Validation of '{0}' has failed due to errors: {1}.";
		public const string UI_UNITYPLUGIN_VALIDATE_RUN_FOR = "Running validation tool for {0}";
		public const string UI_UNITYPLUGIN_VALIDATING_ASSETS = "Validating Assets...";
		public const string UI_UNITYPLUGIN_WINDOW_ASSET_VERSION_LABEL = "Unity Asset Version";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSE_BUTTON = "Browse...";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSER = "Web Browser";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSER_PATH = "Browser Path";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSER_PATH_TITLE = "Path to web browser application";
		public const string UI_UNITYPLUGIN_WINDOW_CANCEL_BUTTON = "Cancel";
		public const string UI_UNITYPLUGIN_WINDOW_CHECK_RESULT_MISSING_MONO_OR_DOTNET = "Missing .NET Runtime!";
		public const string UI_UNITYPLUGIN_WINDOW_CHECKING_MONO = "Checking Mono...";
		public const string UI_UNITYPLUGIN_WINDOW_CHECKING_MONO_FAILED = "No version information returned from Mono.";
		public const string UI_UNITYPLUGIN_WINDOW_CHECKING_VERSION = "Checking...";
		public const string UI_UNITYPLUGIN_WINDOW_DOWNLOAD_DOTNET = "c) Alternatively you can download .NET {0} by clicking 'Download .NET {0}'.";
		public const string UI_UNITYPLUGIN_WINDOW_DOWNLOAD_DOTNET_BUTTON = "Download .NET {0}";
		public const string UI_UNITYPLUGIN_WINDOW_DOWNLOAD_MONO = "b) If it doesn't exist, click 'Download Mono' below and try again.";
		public const string UI_UNITYPLUGIN_WINDOW_DOWNLOAD_MONO_BUTTON = "Download Mono";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_CHECKING_RUNTIME = "Checking runtime and application version...";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_COPYING_EXECUTABLE = "Making shadow copy of tools";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE = "Launching editor's application...";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER = "Opening web browser window...";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_TITLE = "Editor";
		public const string UI_UNITYPLUGIN_WINDOW_EXTENSIONS_LABEL = "Extensions";
		public const string UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT = "Failed to start the Game Data editor due to errors: Aborted by timeout.";
		public const string UI_UNITYPLUGIN_WINDOW_FIND_MONO_MANUALLY = "a) You can manually locate Mono Runtime by clicking 'Browse...' button.";
		public const string UI_UNITYPLUGIN_WINDOW_HELP_BUTTON = "Help";
		public const string UI_UNITYPLUGIN_WINDOW_INFO_GROUP = "Info:";
		public const string UI_UNITYPLUGIN_WINDOW_KILL_PROCESS_BUTTON = "Kill Process";
		public const string UI_UNITYPLUGIN_WINDOW_PATH_TO_MONO = "Path to Mono (bin)";
		public const string UI_UNITYPLUGIN_WINDOW_PRESS_HELP = "If you require help with .NET Runtime installation, click 'Help'.";
		public const string UI_UNITYPLUGIN_WINDOW_RE_CHECK_BUTTON = "Recheck";
		public const string UI_UNITYPLUGIN_WINDOW_RELOAD_BUTTON = "Reload";
		public const string UI_UNITYPLUGIN_WINDOW_RUNTIME_REQUIRED = "You need to have either .NET Runtime {0} or Mono Runtime {1} installed on your system to use Game Data Editor and its related tools.";
		public const string UI_UNITYPLUGIN_MISSING_DOTNET_RUNTIME = "Mono or .NET runtime found on the machine. Please use 'Tools -> Charon -> Troubleshooting -> Check Runtime...' to set up the proper runtime.";
		public const string UI_UNITYPLUGIN_WINDOW_RUNTIME_VERSION = "Runtime Version";
		public const string UI_UNITYPLUGIN_WINDOW_RUNTIME_VERSION_ERROR = "Error";
		public const string UI_UNITYPLUGIN_WINDOW_RUNTIME_VERSION_UNKNOWN = "Unknown";
		public const string UI_UNITYPLUGIN_WINDOW_SETTINGS_GROUP = "Settings:";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_ACTION_COLUMN_NAME = "Action";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_AVAILABLE_TITLE = "Update available!";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_AVAILABLE_VERSION_COLUMN_NAME = "Available";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_NAME = "Charon";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_UNITY_PLUGIN_NAME = "Charon plugin for Unity";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_CHECKING_MESSAGE = "Checking...";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_CURRENT_VERSION_COLUMN_NAME = "Current";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_DOWNLOAD_ACTION = "Download";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_ERROR_MESSAGE = "Error";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_EXPRESSIONS_PLUGIN_NAME = "Expression parsing library";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_PRODUCT_COLUMN_NAME = "Product";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_REPAIR_ACTION = "Repair";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_RUNTIME_TITLE = ".NET Runtime Update";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_SKIP_ACTION = "Skip";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_SKIP_BUTTON = "Skip";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_TEXT_TRANSFORM_PLUGIN_NAME = "Text Transform plugin for Unity";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_TITLE = "Product Updates";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_ACTION = "Update";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_BUTTON = "Update";
		public const string UI_UNITYPLUGIN_WINDOW_UPDATE_REVIEW_UPDATES_BUTTON = "Update...";
		public const string UI_UNITYPLUGIN_WINDOWCHECK_RESULT_MISSING_TOOLS = "Missing Tools!";
		public const string UI_UNITYPLUGIN_SERVER_ERROR = "The request ended with the following errors:";
	}
}

