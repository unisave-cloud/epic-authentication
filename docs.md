Unisave Epic Authentication Documentation
=========================================


## Getting started

- You have your game connected with Unisave
- Import this Unity package (this integration asset)
- Set up EOS account via this guide: https://dev.epicgames.com/docs/epic-online-services/eos-get-started/services-quick-start
- Download the latest EOS SDK and put it into your Unity project (https://dev.epicgames.com/en-US/sdk)
  - NOTE: General information on how to use the SDK from Unity: https://dev.epicgames.com/docs/epic-online-services/eos-get-started/eossdkc-sharp-getting-started
  - Put the SDK into the Plugins folder otherwise it won't be seen by this asset (as Plugins are compiled before everything else). If that isn't an option and you need the EOSSDK to be directly in the `Assets` folder, you can move this asset folder outside of the Plugins folder instead.
  - <img src="https://static-assets-prod.epicgames.com/eos-docs/game-services/c-sharp-getting-started/unity_-2.png">
- ???

## FAQ

**Unity Editor freezes when stopping the game in the SimpleDemo project.**<br>
You have to have all the necessary keys and credentials filled out, then it won't freeze. It's probably an Epic SDK bug. Happened to me on SDK 1.15.5.
