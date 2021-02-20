# GALADE: The Graphical Abstraction Layered Architecture Development Environment

GALADE is a self-generating diagramming tool and code generator that supports the creation, development, and maintenance of ALA applications.

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

#### Editing Properties

#### Adding, Removing, and Moving Nodes

#### Adding, Removing, and Moving Wires
