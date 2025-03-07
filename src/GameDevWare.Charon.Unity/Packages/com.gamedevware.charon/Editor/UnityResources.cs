/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

namespace GameDevWare.Charon.Editor {
	internal static class Resources
	{
		public const string UI_UNITYPLUGIN_CREATE_GAMEDATA_WINDOW_TITLE = "Create Game Data";
		public const string UI_UNITYPLUGIN_CONNECT_WINDOW_TITLE = "Connect Game Data";
		public const string UI_UNITYPLUGIN_ERROR_SCRIPTS_COMPILING = "Interrupted by Unity's script compilation. Retry after Unity has completed script compilation.";
		public const string UI_UNITYPLUGIN_COMPILING_WARNING = "Actions cannot be performed while the Unity Editor is compiling scripts.";
		public const string UI_UNITYPLUGIN_COROUTINE_IS_RUNNING_WARNING = "Actions cannot be performed while another process is running.";
		public const string UI_UNITYPLUGIN_GENERATE_ASSET_CANT_FIND_GAMEDATA_CLASS = "Asset generation failed: Unable to locate the game data type '{0}' in the following assemblies: {1}. Ensure there are no compilation errors.";
		public const string UI_UNITYPLUGIN_GENERATE_CODE_FOR = "Executing source code generator for {0}";
		public const string UI_UNITYPLUGIN_GENERATE_FAILED_DUE_ERRORS = "Source code generation for '{0}' failed due to errors: {1}.";
		public const string UI_UNITYPLUGIN_GENERATE_REFRESHING_ASSETS = "Refreshing assets...";
		public const string UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS = "Generating Code and Assets...";
		public const string UI_UNITYPLUGIN_INSPECTOR_ACTIONS_GROUP = "Actions";
		public const string UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED = "This asset is not connected to an online project. Click the 'Connect' button below to set up the connection.";
		public const string UI_UNITYPLUGIN_INSPECTOR_CLEAR_OUTPUT_DIRECTORY = "Clear Output Directory";
		public const string UI_UNITYPLUGIN_INSPECTOR_SPLIT_FILES = "Split into Multiple Files";
		public const string UI_UNITYPLUGIN_INSPECTOR_PROJECT_LABEL = "Project";
		public const string UI_UNITYPLUGIN_INSPECTOR_BRANCH_LABEL = "Branch";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GAMEDATA_CLASS_NAME = "Game Data Class";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_DOCUMENT_CLASS_NAME = "Document Class";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_DEFINE_CONSTANTS = "Define Constants";
		public const string UI_UNITYPLUGIN_INSPECTOR_REVISION_HASH_LABEL = "Revision Hash";
		public const string UI_UNITYPLUGIN_INSPECTOR_GAME_DATA_VERSION_LABEL = "Game Data Version";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_LABEL = "C# Code Generation Settings";
		public const string UI_UNITYPLUGIN_INSPECTOR_ASSET_IMPORT_SETTINGS_LABEL = "Asset Import Settings";
		public const string UI_UNITYPLUGIN_INSPECTOR_LAST_GENERATED_ASSET_LABEL = "Last Generated Asset";
		public const string UI_UNITYPLUGIN_INSPECTOR_PUBLICATION_LANGUAGES_LABEL = "Languages";
		public const string UI_UNITYPLUGIN_INSPECTOR_PUBLICATION_FORMAT_LABEL = "Format";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_GENERATION_PATH = "Generation Path";
		public const string UI_UNITYPLUGIN_INSPECTOR_GAME_DATA_FILE = "Game Data File";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_INDENTATION = "Indentation";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_LINE_ENDINGS = "Line Endings";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_NAMESPACE = "Namespace";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_OPTIMIZATIONS = "Optimizations";
		public const string UI_UNITYPLUGIN_INSPECTOR_EDIT_BUTTON = "Edit";
		public const string UI_UNITYPLUGIN_INSPECTOR_REIMPORT_BUTTON = "Reimport";
		public const string UI_UNITYPLUGIN_INSPECTOR_OPERATION_DONE = "(Done)";
		public const string UI_UNITYPLUGIN_INSPECTOR_OPERATION_RUNNING = "(Running)";
		public const string UI_UNITYPLUGIN_INSPECTOR_SYNCHRONIZE_BUTTON = "Synchronize";
		public const string UI_UNITYPLUGIN_INSPECTOR_CONNECT_BUTTON = "Connect";
		public const string UI_UNITYPLUGIN_INSPECTOR_RESET_BUTTON = "Reset";
		public const string UI_UNITYPLUGIN_INSPECTOR_DISCONNECT_BUTTON = "Disconnect";
		public const string UI_UNITYPLUGIN_INSPECTOR_SET_API_KEY_BUTTON = "Set API Key";
		public const string UI_UNITYPLUGIN_INSPECTOR_CONNECTION_LABEL = "Connection";
		public const string UI_UNITYPLUGIN_INSPECTOR_NOT_CONNECTED_LABEL = "<Not Connected>";
		public const string UI_UNITYPLUGIN_INSPECTOR_LAUNCHING_EDITOR_PREFIX = "Launching Editor:";
		public const string UI_UNITYPLUGIN_OPERATION_CANCELLED = "Operation was canceled by the user.";
		public const string UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION = "Checking current tools version...";
		public const string UI_UNITYPLUGIN_PROGRESS_PROCESSING_GAMEDATA = "Processing game data at {0}";
		public const string UI_UNITYPLUGIN_PROGRESS_AUTHENTICATING = "Authenticating...";
		public const string UI_UNITYPLUGIN_PROGRESS_IMPORTING = "Importing...";
		public const string UI_UNITYPLUGIN_PROGRESS_MIGRATING = "Migrating...";
		public const string UI_UNITYPLUGIN_PROGRESS_DONE = "Done";
		public const string UI_UNITYPLUGIN_PROGRESS_RUNNING_PRE_TASKS = "Running custom pre-operation tasks...";
		public const string UI_UNITYPLUGIN_PROGRESS_RUNNING_POST_TASKS = "Running custom post-operation tasks...";
		public const string UI_UNITYPLUGIN_PROGRESS_DOWNLOADING = "Downloading '{2}' ({0:F2}/{1:F2} MiB)...";
		public const string UI_UNITYPLUGIN_PROGRESS_UPLOADING = "Uploading '{2}' ({0:F2}/{1:F2} MiB)...";
		public const string UI_UNITYPLUGIN_PROGRESS_DELETING_FILES = "Deleting files...";
		public const string UI_UNITYPLUGIN_SPECIFY_EXTRACTION_LOC_TITLE = "Specify Extraction Location...";
		public const string UI_UNITYPLUGIN_T4_EXTRACTION_COMPLETE = "T4 Template extracted successfully.";
		public const string UI_UNITYPLUGIN_T4_EXTRACTION_FAILED = "T4 Template extraction failed due to errors: {0}.";
		public const string UI_UNITYPLUGIN_VALIDATE_COMPLETE = "Validation of '{0}' is complete. Result: {1}, Errors: {2}.";
		public const string UI_UNITYPLUGIN_VALIDATE_FAILED_DUE_ERRORS = "Validation of '{0}' failed due to errors: {1}.";
		public const string UI_UNITYPLUGIN_VALIDATE_RUN_FOR = "Executing validation tool for {0}";
		public const string UI_UNITYPLUGIN_VALIDATING_ASSETS = "Validating Assets...";
		public const string UI_UNITYPLUGIN_WINDOW_CHARON_EDITOR_VERSION_LABEL = "Charon Editor Version";
		public const string UI_UNITYPLUGIN_ABOUT_IDLE_CLOSE_TIMEOUT_LABEL = "Process Idle Timeout";
		public const string UI_UNITYPLUGIN_ABOUT_SERVER_ADDRESS_LABEL = "Server Address";
		public const string UI_UNITYPLUGIN_ABOUT_EDITOR_APPLICATION_TYPE_LABEL = "Editor Application";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSER_PATH = "Browser Path";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSE_BUTTON = "Browse...";
		public const string UI_UNITYPLUGIN_WINDOW_BROWSER_PATH_TITLE = "Path to Web Browser Application";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE = "Launching editor application...";
		public const string UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER = "Opening web browser window...";
		public const string UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT = "Failed to start the Game Data editor: Operation aborted due to timeout.";
		public const string UI_UNITYPLUGIN_WINDOW_INFO_GROUP = "Info:";
		public const string UI_UNITYPLUGIN_WINDOW_SETTINGS_GROUP = "Settings:";
		public const string UI_UNITYPLUGIN_WINDOW_CHECK_UPDATES_BUTTON = "Check Updates...";
		public const string UI_UNITYPLUGIN_SERVER_ERROR = "The request encountered the following errors:";
		public const string UI_UNITYPLUGIN_GENERATE_API_KEY_TITLE = "API Key";
		public const string UI_UNITYPLUGIN_GENERATE_PROJECT_LABEL = "Project";
		public const string UI_UNITYPLUGIN_GENERATE_BRANCH_LABEL = "Branch";
		public const string UI_UNITYPLUGIN_GENERATE_TARGET_PATH_LABEL = "Target Path";
		public const string UI_UNITYPLUGIN_GENERATE_DOWNLOAD_BUTTON = "Download";
		public const string UI_UNITYPLUGIN_GENERATE_UPLOAD_BUTTON = "Upload";
		public const string UI_UNITYPLUGIN_GENERATE_UPLOAD_LOCAL_GAME_DATA = "Uploading local game data ({0:F2} KiB) to the server.";
		public const string UI_UNITYPLUGIN_GENERATE_LOCAL_ERASED_WARNING = "The local game data file '{0}' will be erased!";
		public const string UI_UNITYPLUGIN_CONNECT_MISSING_OBJECT_WARNING = "Game data is missing!";
		public const string UI_UNITYPLUGIN_INSPECTOR_CODE_MISSING_GAMEDATA_FILE = "No game data file is linked to this asset!";
		public const string UI_UNITYPLUGIN_GENERATE_API_KEY_MESSAGE = "To generate a new API key, click *here* to open the browser...";
		public const string UI_UNITYPLUGIN_CREATE_GAMEDATA_NAME_LABEL = "Name";
		public const string UI_UNITYPLUGIN_CREATE_GAMEDATA_FORMAT_LABEL = "Format";
		public const string UI_UNITYPLUGIN_CREATE_GAMEDATA_FOLDER_LABEL = "Folder";
		public const string UI_UNITYPLUGIN_CREATE_GAMEDATA_CREATE_BUTTON = "Create";
		public const string UI_UNITYPLUGIN_CREATING_GAME_DATA_NO_STREAMING_ASSETS = "Game data cannot be created in the 'StreamingAssets' folder because source code cannot be placed there. The JSON/MessagePack data file can be moved there manually if necessary.";
		public const string UI_UNITYPLUGIN_CREATING_GAME_DATA_INVALID_NAME = "Invalid game data name '{0}'. The name must consist only of Latin letters and underscores, contain no spaces, and must not start with a number or an underscore.";
		public const string UI_UNITYPLUGIN_CREATING_GAME_DATA_IS_USED = "This name is already in use for an asset at '{0}'.";
		public const string UI_UNITYPLUGIN_CREATING_PROGRESS_INIT_GAMEDATA = "Initializing the game data file. This may take time initially.";
		public const string UI_UNITYPLUGIN_CREATING_GAMEDATA_ASSET = "Creating a game data asset file.";
		public const string UI_UNITYPLUGIN_GENERATING_SOURCE_CODE = "Generating C# source code for a game data asset.";
	}
}

