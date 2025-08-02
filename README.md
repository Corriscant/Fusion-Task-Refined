# Photon Fusion Test Sample

This project is a small test of Photon Fusion in a Host-Client mode.

It was created to demonstrate code culture and style. Initially, it was a test assignment for Tense Games (which was successfully completed). Later, it was refactored to test some new technologies (OpenAI Codex), to verify the migration process to a newer Photon Fusion version (2.0.3 -> 2.0.6), and to create cleaner code.

## Prerequisites

* Unity 6000.0.24f1
* [Photon Fusion 2.0.6](https://www.photonengine.com/fusion)

## Installation

1.  **Clone the repository:**
    ```sh
    git clone [https://github.com/Corriscant/Fusion-Task-Refined.git](https://github.com/Corriscant/Fusion-Task-Refined.git)
    ```
2.  **Open the project in Unity.** Unity will complain about missing dependencies. This is expected.
3.  **Install Photon Fusion:**
    * Download **Fusion SDK v2.0.6** from the [Photon Engine website](https://www.photonengine.com/sdks#fusion).
    * In the Unity Editor, go to **Assets -> Import Package -> Custom Package...**
    * Select the downloaded `fusion.v2.0.6.unitypackage` file and import all assets.

## Configuration

After installation, you need to verify the Photon App ID.

1.  In the Unity Editor, open the Fusion Hub by navigating to **Tools > Fusion > Fusion Hub > Welcome**.
2.  The project includes a pre-configured demo App ID, which you will see in the **Fusion App Id** field.
3.  This ID is provided for convenience and operates on a free plan with a limit of 20 concurrent users. If you intend to use this project as a basis for your own, please replace it with your own App ID from the [Photon Dashboard](https://dashboard.photonengine.com/en-US/account/signin).

## How to Test

To test the multiplayer functionality, you need to run at least two instances of the application: one as the **Host** and one as the **Client**.

1.  **Create a Build:**
    * In the Unity Editor, go to **File -> Build Profiles**
	* Platforms: **Windows**
    * Click **"Build"** and choose a location to save the standalone application.

2.  **Run Host & Client:**
    * **Run the build** you just created. This will act as your **Client**.
    * Back in the **Unity Editor**, press the **Play** button. This will act as your **Host**.
	* Or you can just run some EXE instances of the Build you just created.

3.  **Connect:**
    * In the Host instance (Unity Editor), click the "Host" button.
    * In the Client instance (the build), click the "Join" button to connect to the host.
