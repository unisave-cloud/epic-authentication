Unisave Epic Authentication
===========================

<a href="https://unisave.cloud/" target="_blank">
    <img alt="Website" src="https://img.shields.io/badge/Website-unisave.cloud-blue">
</a>
<a href="https://discord.gg/XV696Tp" target="_blank">
    <img alt="Discord" src="https://img.shields.io/discord/564878084499832839?label=Discord">
</a>

This repository contains the Unisave Epic Authentication module sources. To get started, read the [documentation page](https://unisave.cloud/docs/epic-authentication).


Setting up development
----------------------

- Clone this repository
- Open in Unity, check Unity version in [ProjectSettings/ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)
- Import the [Unisave asset](https://assetstore.unity.com/packages/slug/142705) from the asset store
- Import Text Mesh Pro `Window > TextMeshPro > Import TMP Essential Resources`
- Install the [Epic Online Services SDK](https://dev.epicgames.com/en-US/sdk) in the way described in the [documentation](https://unisave.cloud/docs/epic-authentication#installation)
- Set up Unisave cloud connection so that the examples can be compiled and executed


Deployment checklist
--------------------

- Set the proper version in the `ModuleMeta.cs` file.
- In Unity, right click the `Assets/Plugins/UnisaveEpicAuthentication` folder and click `Export package...`
- Untick the `Include Dependencies` checkbox to get rid of all the unwanted surrounding files
- Export it as `unisave-epic-authentication-0.0.0.unitypackage`
- Commit the version change as a `v0.0.0` commit
- Create new GitHub release for the version and include the `.unitypackage` in it
- Update the header (version, package link) of [the documentation page](https://unisave.cloud/docs/epic-authentication) and release the updated website
