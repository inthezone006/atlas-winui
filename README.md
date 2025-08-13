# ATLAS 🗺️

ATLAS is a modern Windows desktop application designed to be your digital map to online safety. It provides a suite of tools to help users detect and understand potential online scams by analyzing text, voice, and image content.

## About The Project

In the current age of widespread misinformation and sophisticated scams, ATLAS empowers users to navigate the digital world with confidence. Using AI and machine learning models, ATLAS examines content you provide for common patterns and indicators of malicious intent. Whether it's a suspicious email, a strange voicemail, or a questionable image from social media, our platform provides a clear analysis to help you make informed decisions and stay safe.

### Built With

* **Frontend:** C# with WinUI 3 & the Windows App SDK
* **Backend:** Python with Flask
* **Database:** MongoDB

---

## Getting Started

Follow these instructions to set up a local development environment for ATLAS.

### Prerequisites

Before you begin, ensure you have the following software installed on your machine:

1.  **Visual Studio 2022**
    * During installation, you **must** include the **".NET Multi-platform App UI development"** workload. This will install the necessary components for WinUI 3 and the Windows App SDK. 
2.  **Windows Developer Mode**
    * You must enable Developer Mode in your Windows settings to run and debug the application.
    * Go to **Settings** > **Privacy & security** > **For developers** and turn **Developer Mode** on.
3.  **Python 3.8+**
    * Download and install from [python.org](https://www.python.org/). Make sure to add Python to your system's PATH.
4.  **MongoDB**
    * Install a local instance of MongoDB Community Server or use a cloud-based service like MongoDB Atlas.

### Installation

The project is split into a frontend (the WinUI 3 app) and a backend (the Flask server). Both must be running for the app to function correctly.

#### 1. Backend Setup (Flask API)

1.  **Clone the repository:**
    ```sh
    git clone [https://github.com/your-username/your-repository-name.git](https://github.com/your-username/your-repository-name.git)
    cd your-repository-name/backend-folder
    ```
2.  **Create and activate a virtual environment:**
    ```sh
    # Windows
    python -m venv venv
    .\venv\Scripts\activate

    # macOS/Linux
    python3 -m venv venv
    source venv/bin/activate
    ```
3.  **Install Python dependencies:**
    ```sh
    pip install -r requirements.txt
    ```
4.  **Configure Environment Variables:**
    * Set your Flask secret key and any database connection strings as required by the backend code.
5.  **Run the Flask server:**
    ```sh
    flask run
    ```
    The backend should now be running, typically on `http://127.0.0.1:5000`.

#### 2. Frontend Setup (WinUI 3 App)

1.  **Open the Solution:**
    * Navigate to the frontend project folder and open the `.sln` file with Visual Studio 2022.
2.  **Restore NuGet Packages:**
    * Visual Studio should automatically restore the required packages. If not, right-click the solution in the Solution Explorer and select "Restore NuGet Packages".
3.  **Verify Backend URL:**
    * Ensure the API URLs in the C# page files (e.g., `LoginPage.xaml.cs`, `TextAnalysisPage.xaml.cs`) point to your local Flask server (`http://127.0.0.1:5000`).

---

## Usage ✈️

1.  **Start the backend server** using the `flask run` command as described above.
2.  In Visual Studio, set the solution configuration to **Debug** and the platform to **x64**.
3.  Press the **Run** button (the green play icon) or **F5** to build and launch the ATLAS application.
