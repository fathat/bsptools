# Solid Leaf BSP Compiler for Unity3D


## What is this for?

Filling a mesh of static level geometry with convex colliders to define
zones.

I needed a way to determine if an object is "inside" or "outside" 
a specific zone. In Unity3D, triggers need to be convex, so
this exists to decompose an arbitrary mesh into a set of convex
colliders.

## How to use

Here's an example of a mesh defining some hallways. 
(Backface culling is enabled, but this mesh has a roof as well).

![Hallways](Screenshots/original.png?raw=true "Hallways")

If we want to determine if an object is inside or outside this
structure, we'd generally use colliders marked as triggers. However, 
a single collider cannot represent this structure, so we would have
to create multiple invisible colliders by hand that would roughly follow the 
shape of this:

![Hand editing](Screenshots/handmade.png?raw=true "Hand editing")

While this is certainly viable, it's also time consuming and inaccurate.

With this code, we can automate this. If you go to the Window menu
and bring up "BSP Tools", we can select this mesh, and then press "Fill
With Convex Colliders". As a result, we'll get something like this:

![Hallways With Colliders](Screenshots/result.png?raw=true "Colliders")

You'll notice the colliders (green highlights) follow the mesh exactly, 
and each collider is a convex hull. In this case it generated 11 colliders
for our hallway scene.

Here's a picture of the same thing, but with "debug visuals" enabled
to show the exact way the mesh was subdivided:

![Hallways With Colliders](Screenshots/deconstructed.png?raw=true "Colliders")

The mesh is slightly pulled apart for clariy, but you can see each 
"zone" that has been created and assigned a color.

## Options

| Option Name    |       Purpose         |
|----------------|-----------------------|
| Create Debug Visuals | Adds a MeshRenderer component to generated colliders. Useful for debugging results. |
| Create Colliders as Triggers | Controls if IsTrigger is enabled or disabled on colliders |
| High Quality   | If enabled, tries to generate an optimal BSP tree. If disabled, generates a tree more quickly (but less optimally) |
| Steps Per Frame | On large models this operation can take a while, so it is run asynchronously so as not to freeze the editor. More steps will finish the process faster, but make the editor less responsive |

## License

This code is public domain. Please use for any purpose you wish.