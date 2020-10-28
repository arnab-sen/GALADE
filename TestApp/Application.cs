            Vertical id_4b575b5cb18b4eaf9d0eba0959da9dab = new Vertical() {  };
            CanvasDisplay id_2152b761938046cda7787f184716ba65 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_a480790424c040098d4adee302321fb6 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_ed5587c10f6c43bfb22a2efcba4278ce = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A } };
            Data<object> id_99df84ddc4144b4c8ebe9a9454e04079 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;return node;} };
            ApplyAction<object> id_9ac6d1f88493445aab7e7c713451179e = new ApplyAction<object>() { Lambda = input =>{(input as ALANode).CreateInternals();var render = (input as ALANode).Render;mainCanvas.Children.Add(render);WPFCanvas.SetLeft(render, 20);WPFCanvas.SetTop(render, 20);} };

            mainWindow.WireTo(id_4b575b5cb18b4eaf9d0eba0959da9dab, "iuiStructure");
            id_4b575b5cb18b4eaf9d0eba0959da9dab.WireTo(id_2152b761938046cda7787f184716ba65, "children");
            id_2152b761938046cda7787f184716ba65.WireTo(id_a480790424c040098d4adee302321fb6, "canvasOutput");
            id_2152b761938046cda7787f184716ba65.WireTo(id_ed5587c10f6c43bfb22a2efcba4278ce, "eventHandlers");
            id_ed5587c10f6c43bfb22a2efcba4278ce.WireTo(id_99df84ddc4144b4c8ebe9a9454e04079, "eventHappened");
            id_99df84ddc4144b4c8ebe9a9454e04079.WireTo(id_9ac6d1f88493445aab7e7c713451179e, "dataOutput");

            Vertical id_4b575b5cb18b4eaf9d0eba0959da9dab = new Vertical() {  };
            CanvasDisplay id_2152b761938046cda7787f184716ba65 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_a480790424c040098d4adee302321fb6 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_ed5587c10f6c43bfb22a2efcba4278ce = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A } };
            Data<object> id_99df84ddc4144b4c8ebe9a9454e04079 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;return node;} };
            ApplyAction<object> id_9ac6d1f88493445aab7e7c713451179e = new ApplyAction<object>() { Lambda = input =>{(input as ALANode).CreateInternals();var render = (input as ALANode).Render;mainCanvas.Children.Add(render);WPFCanvas.SetLeft(render, 20);WPFCanvas.SetTop(render, 20);} };

            mainWindow.WireTo(id_4b575b5cb18b4eaf9d0eba0959da9dab, "iuiStructure");
            id_4b575b5cb18b4eaf9d0eba0959da9dab.WireTo(id_2152b761938046cda7787f184716ba65, "children");
            id_2152b761938046cda7787f184716ba65.WireTo(id_a480790424c040098d4adee302321fb6, "canvasOutput");
            id_2152b761938046cda7787f184716ba65.WireTo(id_ed5587c10f6c43bfb22a2efcba4278ce, "eventHandlers");
            id_ed5587c10f6c43bfb22a2efcba4278ce.WireTo(id_99df84ddc4144b4c8ebe9a9454e04079, "eventHappened");
            id_99df84ddc4144b4c8ebe9a9454e04079.WireTo(id_9ac6d1f88493445aab7e7c713451179e, "dataOutput");

            Vertical id_4b575b5cb18b4eaf9d0eba0959da9dab = new Vertical() {  };
            CanvasDisplay id_2152b761938046cda7787f184716ba65 = new CanvasDisplay() { Width = 1920, Height = 600, Background = Brushes.White, StateTransition = stateTransition };
            ApplyAction<System.Windows.Controls.Canvas> id_a480790424c040098d4adee302321fb6 = new ApplyAction<System.Windows.Controls.Canvas>() { Lambda = canvas => mainCanvas = canvas };
            KeyEvent id_ed5587c10f6c43bfb22a2efcba4278ce = new KeyEvent(eventName: "KeyDown") { Keys = new[] { Key.A } };
            Data<object> id_99df84ddc4144b4c8ebe9a9454e04079 = new Data<object>() { Lambda = () => {var node = new ALANode();node.Graph = mainGraph;node.Canvas = mainCanvas;return node;} };
            ApplyAction<object> id_9ac6d1f88493445aab7e7c713451179e = new ApplyAction<object>() { Lambda = input =>{(input as ALANode).CreateInternals();var render = (input as ALANode).Render;mainCanvas.Children.Add(render);WPFCanvas.SetLeft(render, 20);WPFCanvas.SetTop(render, 20);} };

            mainWindow.WireTo(id_4b575b5cb18b4eaf9d0eba0959da9dab, "iuiStructure");
            id_4b575b5cb18b4eaf9d0eba0959da9dab.WireTo(id_2152b761938046cda7787f184716ba65, "children");
            id_2152b761938046cda7787f184716ba65.WireTo(id_a480790424c040098d4adee302321fb6, "canvasOutput");
            id_2152b761938046cda7787f184716ba65.WireTo(id_ed5587c10f6c43bfb22a2efcba4278ce, "eventHandlers");
            id_ed5587c10f6c43bfb22a2efcba4278ce.WireTo(id_99df84ddc4144b4c8ebe9a9454e04079, "eventHappened");
            id_99df84ddc4144b4c8ebe9a9454e04079.WireTo(id_9ac6d1f88493445aab7e7c713451179e, "dataOutput");
