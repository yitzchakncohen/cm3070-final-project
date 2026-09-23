## Guide for Creating a New Vehicle

**1.** Copy one of the provided template vehicles (`Assets -> Vehicles -> Prefabs`).

**2.** Create a new **VehicleConfiguration** scriptable object and assign it to the vehicle controller (`Assets -> Vehicles -> Data -> Vehicles`) in the vehicle prefab.

**3.** Create or use existing scriptable object vehicle components and assign them to the vehicle configuration using the serialized inspector fields.

**4.** Set your default values for the vehicle component configurations in the scriptable object fields or in the runtime UI Vehicle Configuration screen.

**5.** Open the vehicle prefab, and under the `Chassis` parent, delete any existing game objects under the `Body/Model` parent and add your vehicle body prefab.

* **a.** Ensure this prefab has any relevant colliders.
* **b.** ***Note that body and wheel meshes should have read/write enabled.***
* **c.** Ensure the vehicle is positioned with the correct ground clearance.
* **d.** Move the mirror camera prefabs to be positioned correctly with respect to the vehicle model’s mirror. 

**6.** Under the `Wheels` parent, replace the existing wheels with the wheel prefabs of your choice or create your own by copying an existing wheel and modifying it.

* **a.** Update the wheel objects with the correct boolean values for **motorized**, **steerable**, and **front**.
* **b.** Remove any colliders from the wheel prefabs.
* **c.** Position the wheels correctly based on the configured track and wheelbase.
* **d.** Adjust the rotation of the tire model parent and scale of the wheel mesh that is childed to the tire model parent. *(This can be the inverse of the tire parent scale when the vehicle is at rest to ensure the wheels do not appear deformed by default.)*
* **e.** On the wheels parent, add the values to the front or back roll bars where relevant.

**7.** Drop your vehicle into the demo scene or your own scene and tune it to your liking.