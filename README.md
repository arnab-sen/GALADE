# GALADE: The Graphical Abstraction Layered Architecture Development Environment

GALADE is a self-generating diagramming tool and code generator that handles the creation, development, and maintenance of ALA applications.

At the moment, only ALA applications written in C# are supported.

See https://abstractionlayeredarchitecture.com/ for more details on ALA.


## Getting Started Guide

This guide will use the following project as an example: https://github.com/arnab-sen/ALASandbox

### Visualising an Existing ALA Project
First, use `File > Open Project` to locate the ALASandbox folder. This will cause GALADE to look through the `ALACore/ProgrammingParadigms` and `ALACore/DomainAbstractions` folders, parse any C# classes and interfaces found, and create internal models of abstractions that can be visualised.

Next, open `ALASandbox/Application.cs`. This file contains the code necessary to produce the diagram. If  you view the file in a text editor, you can see that it has landmarks in the form of comments to indicate where GALADE should insert generated code:

![](https://i.gyazo.com/63ed8e346133a9e1a4086cf0fabc91e0.png)

Opening it via GALADE should show the following diagram:

![](https://i.gyazo.com/fe4ad1a50837fd67a8ba214601539748.png)

### Modifying the Diagram
There are two ways to add a new node:

1. Right click > `Add node`: This will add a floating node to the diagram. They will be treated as roots when the layout manager calls an update (which can be manually done by middle clicking on the background and pressing R).
2. Select a port, then press `A` to add a new node as a child, connected to that port.

Then, all abstraction models found can be loaded into the new node by selecting from, or typed into, the type drop down menu.

The following video showcases everything mentioned in this section:
![](https://i.gyazo.com/edffe43b3a634dd16c2c3039eb34b094.gif)


#### Editing Properties
A node can be modified in the following ways:

* Modify its type by using the type dropdown. Any changes made to the current type will be saved in the node in the current session, so if you accidentally change its type, you can change back without losing any information.

    ![](https://i.gyazo.com/1d9c97898f74289fb82126e6ddc92ae4.gif)
* By default, all nodes are given a unique id (based on a GUID), and its variable name when generated will be based on this id. The name textbox will appear blank when this is the case. If you want, you can change the variable name into something more human-readable via the name text box.
* If the node's class requires type parameters, dropdowns for them will appear beside the type drop down, and can be modified to your liking.
* Press the `+` button to add a new member row, and any `-` button to delete its member row. Any changes made to that member will also be saved and can be returned to in case it was hidden by accident.
    
    ![](https://i.gyazo.com/62970f0d0980c8b0e2e208ccd0f7713a.gif)

#### Adding, Removing, and Moving Nodes

#### Adding, Removing, and Moving Wires
