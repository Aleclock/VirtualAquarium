#Requirements

##Goal:
Goal of the project is to create a virtual aquarium where different fishes moves around inside a 3D scene.

##Step-by-step:
1. within Assets/Animations there's AC_FishBase.controller, that represent the animation controller for all the fishes.
2. then, for each fish, i create an override controller, based on the base one.
2.1 You should first create all the overidde controller for all the fishes, linking the corrisponding animation assets. Right know i placed them inside Assets/Resources, within each fish 3d model folder.
3. Then I would like you to fill the FishCatalogSO, defining the scriptable object with the id of the fish, and fill out the parameters. Here, you could decide the school of fishes, base the choice on the real life behaviour of such fishes.
4. Integrate the fish catalog in the 3d scene (create it if not existing) so that when i play the scene, i see the fishes swimming around in the aquarium.
4.1 For now let's keep the scenario simple, the skybox should be based on Shaders/SkyboxGradientShader.shader, while the floor (sand) should be based on Shaders/Black2TransparentShader.