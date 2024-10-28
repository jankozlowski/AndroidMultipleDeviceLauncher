# AndroidMultipleDeviceLauncher

Tool for quick uploading build android apk file to multiplle devices. Useful for testing and quick debugging android native apps, xamarin forms and Maui android apps on multiple devices.

**This extension works as follows:**
- user selects on which devices apk file should be uploaded (emulators and real devices), available devices are read by the use of Android Debug Bridge and Android Virtual Devices,
- after clicking run, extension checks which devices are running and starts emulators if needed,
- based on extension settings, extension will build project from scratch or just take earlier generated apk file from bin folder. Android project must be selected as startup project. Extension determines to create/select debug and release version based on current project configuration, 
- after build extension is searching for apk-Signed.apk file,
- with use of adb, apk file is installed on selected devices and run.

**Download:**

Extension is available for download from Visual Studio Marketplace at https://marketplace.visualstudio.com/items?itemName=BinaryAlchemist.AndroidMultipleDeviceLauncher

**How to use:**

After installation go to View -> Toolbars -> Run Multiple Devices

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/10/MultipleDeviceUploader.png" alt="MultipleDeviceUploader1"/>

At Visual Studio toolbar, there should be new options available. Run Multiple Devices and Settings.

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/10/MultipleDeviceUploader2.png" alt="MultipleDeviceUploader2"/>

In Settings. You will have to provide a path to the folder where Android Debug Bridge is located (Default: C:\Program Files (x86)\Android\android-sdk\platform-tools) and avd Emulator (Default: C:\Program Files (x86)\Android\android-sdk\emulator\). Build checkbox is optional. This parameter is responible if project should be always build before uploading to devices (more time consuming)

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/10/MultipleDeviceUploader3.png" alt="MultipleDeviceUploader3"/>

Below settings is list of found real devices and available virtual devices from which we can select on which devices apk file will be uploaded. 

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/10/MultipleDeviceUploader6.png" alt="MultipleDeviceUploader4"/>

Clicking on play button or Run Multiple Devices will start to upload apk file to selected devices. 

<img src="https://binaryalchemist.pl/wp-content/uploads/2024/10/MultipleDeviceUploader5.png" alt="MultipleDeviceUploader5"/>

**Feedback:**

Any issues and enhancement suggestions can be submitted on github project page
