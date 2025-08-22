# ATLAS 🗺️

ATLAS is a modern Windows desktop application designed to be your digital map to online safety. It provides a suite of tools to help users detect and understand potential online scams by analyzing text, voice, image, and link content. 🛡️

## About The Project ℹ️

In the current age of widespread misinformation and sophisticated scams, ATLAS empowers users to navigate the digital world with confidence. Using AI and machine learning models, ATLAS examines content you provide for common patterns and indicators of malicious intent. Whether it's a suspicious email 📧, a strange voicemail 🎙️, or a questionable image 🖼️, our platform provides a clear analysis to help you make informed decisions.

This repository contains the **frontend client** for ATLAS, built with WinUI 3. The **backend server** is located in a separate repository: [https://github.com/inthezone006/atlas](https://github.com/inthezone006/atlas).

### Built With 🛠️

* **Frontend:** C# with WinUI 3 & the Windows App SDK
* **Backend:** Python with Flask
* **Database:** MongoDB

---

## Features ✨

* 🔍 **Multi-Modal Analysis:** Scan text, audio files, images, and links for potential threats.
* 💻 **On-Device Protection:** Includes a file scanner powered by VirusTotal to check local files.
* 👤 **User Accounts:** Full authentication system with persistent login sessions.
* 📊 **Interactive Dashboard:** View personal statistics and contribute to the community.
* 🎨 **Modern UI:** A clean, responsive interface featuring Mica transparency and custom animations.
* 🌓 **Light/Dark Mode:** Full support for theme switching.

---

## Getting Started 🏁

Follow these instructions to set up the ATLAS client for local development.

### Prerequisites 📋

Before you begin, ensure you have the following software installed:

1.  **Visual Studio 2022** 🧑‍💻
    * You **must** include the **".NET Multi-platform App UI development"** workload.
2.  **Windows Developer Mode** ⚙️
    * Go to **Settings** > **Privacy & security** > **For developers** and turn **Developer Mode** on.

### Installation & Setup ⚙️

1.  **Clone the Frontend Repository:**
    ```sh
    git clone [https://github.com/inthezone006/atlas-winui.git](https://github.com/inthezone006/atlas-winui.git)
    cd atlas-winui
    ```
2.  **Open the Solution:**
    * Open the `ATLAS.sln` file with Visual Studio 2022. Visual Studio should automatically restore the required NuGet packages.
3.  **Connect to the Backend:** 🔗
    * For ease of development, you can connect the client directly to the live, deployed backend.
    * Go through the C# files in the `Pages` folder and ensure all API URLs point to:
        `https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/`
    * **(Optional) Run Local Backend:** If you wish to run the backend locally, clone the [backend repository](https://github.com/inthezone006/atlas) and follow the setup instructions in its `README` file. Remember to update the API URLs in the WinUI 3 app to `http://127.0.0.1:5000`.

---

## Usage 🚀

1.  Ensure you have configured the backend URL in the C# code.
2.  In Visual Studio, set the solution configuration to **Debug** and the platform to **x64**.
3.  Press the **Run** button (the green play icon ▶️) or **F5** to build and launch the ATLAS application.