var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub", {
    accessTokenFactory: () => this.loginToken
}).build();

var chatBtn = document.getElementById("ChatButton");
var inventoryBtn = document.getElementById("InventoryButton");
var menuBtn = document.getElementById("MenuButton");
var slider = document.getElementById("ScaleSlider");
var imgSize = 150;

var chatInput = document.getElementById("chatInput");
var isTyping = false;
chatInput.hidden = true;

var gameCanvas = document.getElementById("game");
var entityCanvas = document.getElementById("entity");
var playerCanvas = document.getElementById("player");
var canvasCanvas = document.getElementById("canvas");
var drawingCanvas = document.getElementById("drawing");
var drawingDiv = document.getElementById("drawingTools");
var directionCanvas = document.getElementById("direction");

var closeDrawingCanvas = document.getElementById("CloseDrawingCanvas");
var clearDrawingCanvas = document.getElementById("ClearDrawingCanvas");

var maxSpritesX = Math.floor(gameCanvas.width / imgSize);
var maxSpritesY = Math.floor(gameCanvas.height / imgSize);
var radiusX = Math.floor(maxSpritesX / 2);
var radiusY = Math.floor(maxSpritesY / 2);
var worldCanvasOriginX, worldCanvasOriginY;
var worldOriginM, worldOriginN;

var inventoryTable = document.getElementById("InventoryTable");
var inventoryItemNames = new Array(25);
var inventoryItemAmounts = new Array(25);
var hotbarTable = document.getElementById("HotbarTable");
var hotbarPosition = 0;
var hotbarItemNames = new Array(5);
var hotbarItemAmounts = new Array(5);
var selectedInventoryY = 0;
var selectedInventoryX = 0;
var selectedHotbarItem = 0;
var swappingItems = false;
var selectedContainer = "";

var playerX, playerY;
var worldTargetM, worldTargetN;
var mouseTargetY, mouseTargetX;
var tileInUseM, tileInUseN;
var username = document.getElementById("Username").value;
var canMove = true;
var playerDirection = "South";

var isDrawing = false;
var lastY, lastX;

var isUsingTile = false;
var stoppedUsingTile = false;

inventoryTable.hidden = true;
hotbarTable.hidden = false;

PositionUI();
RefreshCanvasVariables();
SelectHotbar(hotbarPosition);

if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
    hotbarTable.hidden = true;
}

function modulo(a, b) {
    if (a % b >= 0) {
        return Math.abs(a % b);
    } else {
        return (a % b + b);
    }
}

function removeAllChildren(parent) {
    while (parent.firstChild) {
        parent.removeChild(parent.firstChild);
    }
}

function RefreshCanvasVariables() {
    maxSpritesX = Math.floor(gameCanvas.width / imgSize);
    maxSpritesY = Math.floor(gameCanvas.height / imgSize);
    radiusX = Math.floor(maxSpritesX / 2);
    radiusY = Math.floor(maxSpritesY / 2);
}

function LoadInventory() {
    var itemCounter = 0;

    for (let i = 0; i < 5; i++) {
        for (let j = 0; j < 5; j++) {
            var td = document.getElementById("i" + i + j);
            var img = document.getElementById("ii" + i + j);
            var p = document.getElementById("ip" + i + j);

            img.src = "/images/items/" + inventoryItemNames[itemCounter] + ".png";
            p.innerHTML = inventoryItemAmounts[itemCounter];
            itemCounter++;
        }
    }
}

function LoadHotbar() {
    for (let i = 0; i < 5; i++) {
        var td = document.getElementById("h" + i);
        var img = document.getElementById("hi" + i);
        var p = document.getElementById("hp" + i);

        img.src = "/images/items/" + hotbarItemNames[i] + ".png";
        p.innerHTML = hotbarItemAmounts[i];
    }
}

function SelectInventory(i, j) {
    if (swappingItems) {
        if (selectedContainer == "Inventory") {
            var inventoryIndexA = selectedInventoryY * 5 + selectedInventoryX;
            var inventoryIndexB = i * 5 + j;
            connection.invoke("SwapItems", selectedContainer, "Inventory", inventoryIndexA, inventoryIndexB)
                .catch(function (err) {
                    return console.error(err.toString());
                });
            swappingItems = false;
        } else if (selectedContainer == "Hotbar") {
            var inventoryIndexB = i * 5 + j;
            connection.invoke("SwapItems", selectedContainer, "Inventory", selectedHotbarItem, inventoryIndexB)
                .catch(function (err) {
                    return console.error(err.toString());
                });
            swappingItems = false;
        }  
    } else {
        selectedInventoryY = i;
        selectedInventoryX = j;
        selectedContainer = "Inventory";
        swappingItems = true;
    }
}

function SelectHotbarSwap(pos) {
    if (swappingItems) {
        if (selectedContainer == "Inventory") {
            var inventoryIndexA = selectedInventoryY * 5 + selectedInventoryX;
            connection.invoke("SwapItems", selectedContainer, "Hotbar", inventoryIndexA, pos)
                .catch(function (err) {
                    return console.error(err.toString());
                });
            swappingItems = false;
        } else if (selectedContainer == "Hotbar") {
            connection.invoke("SwapItems", selectedContainer, "Hotbar", selectedHotbarItem, pos)
                .catch(function (err) {
                    console.error(err.toString());
                });
            swappingItems = false;
        }
    } else {
        selectedHotbarItem = pos;
        selectedContainer = "Hotbar";
        swappingItems = true;
    }
}

function SelectHotbar(pos) {
    var td = document.getElementById("h" + hotbarPosition);
    td.style.backgroundColor = "white";

    td = document.getElementById("h" + pos);
    td.style.backgroundColor = "blue";
    hotbarPosition = pos;
}

function UseTile() {
    stoppedUsingTile = false;
    tileInUseM = worldTargetM;
    tileInUseN = worldTargetN;
    connection.invoke("GetTileUse", tileInUseM, tileInUseN).catch(function (err) {
        return console.error(err.toString());
    });
}

function StopUsingTile() {
    stoppedUsingTile = true;
    isUsingTile = false;

    drawingDiv.hidden = true;
    isDrawing = false;

    connection.invoke("StopUsingTile").catch(function (err) {
        return console.error(err.toString());
    });
}

function UseItem(itemName, index, action) {
    var ctx = entityCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, entityCanvas.width, entityCanvas.height);

    img = document.getElementById(itemName + ".png");
    ctx.drawImage(img, mouseTargetX - imgSize/2, mouseTargetY - imgSize/2, imgSize, imgSize);

    connection.invoke("GetItemUse", action, itemName, index, worldTargetM, worldTargetN).catch(function (err) {
        return console.error(err.toString());
    });
}

function PositionUI() {
    gameCanvas.width = window.innerWidth;
    gameCanvas.height = window.innerHeight;
    entityCanvas.width = window.innerWidth;
    entityCanvas.height = window.innerHeight;
    playerCanvas.width = window.innerWidth;
    playerCanvas.height = window.innerHeight;
    canvasCanvas.width = window.innerWidth;
    canvasCanvas.height = window.innerHeight;

    drawingCanvas.width = window.innerWidth / 2;
    drawingCanvas.height = window.innerHeight / 2;
    drawingCanvas.style.left = window.innerWidth / 4;
    drawingCanvas.style.top = window.innerHeight / 4;

    closeDrawingCanvas.style.left = (window.innerWidth / 4) - 100;
    closeDrawingCanvas.style.top = window.innerHeight / 4 + 50;
    clearDrawingCanvas.style.left = (window.innerWidth / 4) - 100;
    clearDrawingCanvas.style.top = (window.innerHeight / 4) + 100;

    directionCanvas.width = window.innerWidth;
    directionCanvas.height = window.innerHeight;

    var ctx = drawingCanvas.getContext("2d");
    ctx.fillStyle = "black";
    ctx.fillRect(0, 0, drawingCanvas.width, drawingCanvas.height);

    var canvasCoords = gameCanvas.getBoundingClientRect();

    inventoryTable.style.top = canvasCoords.top + 100;
    inventoryTable.style.left = canvasCoords.left;

    //hotbarTable.style.top = '200px';
    //hotbarTable.style.left = '200px';

    chatInput.style.left = imgSize * radiusX;
    chatInput.style.top = imgSize * radiusY;
}

function DrawWorld(world, numLayers, worldRows, worldCols, centerX, centerY) {
    var ctx = gameCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, gameCanvas.width, gameCanvas.height);
    ctx.font = "15px Arial";

    var m = Math.floor(centerY - radiusY);
    var n = Math.floor(centerX - radiusX); // top left [m][n] of the game world- [3][3] for example

    var offsetY = (centerY - Math.floor(centerY)) * imgSize;
    var offsetX = (centerX - Math.floor(centerX)) * imgSize;

    var setOrigin = false;

    for (let y = 0 - offsetY; y < gameCanvas.height; y += imgSize) {
        for (let x = 0 - offsetX; x < gameCanvas.width; x += imgSize) {
            if (m < 0 || n < 0) {
                img = document.getElementById("Air.png");
                ctx.drawImage(img, x, y, imgSize, imgSize);
            } else if (m >= worldRows || n >= worldCols) {
                img = document.getElementById("Air.png");
                ctx.drawImage(img, x, y, imgSize, imgSize);

            } else {
                if (!setOrigin) {
                    worldCanvasOriginY = y;
                    worldCanvasOriginX = x;
                    worldOriginM = m;
                    worldOriginN = n;
                    setOrigin = true;
                }
                for (let l = 0; l < numLayers; l++) {
                    if (m == worldTargetM && n == worldTargetN) {
                        ctx.strokeRect(x, y, imgSize, imgSize);
                    }
                    img = document.getElementById(world[l][m][n]);
                    ctx.drawImage(img, x, y, imgSize, imgSize);
                    //ctx.strokeText("[" + m + "][" + n + "]", x, y);
                    //ctx.strokeText("x:" + x.toFixed(2) + " y:" + y.toFixed(2), x, y);
                }
            }
            n++;
        }
        n = Math.floor(centerX - radiusX);
        m++;
    }

    img = document.getElementById("Player.png");
    ctx.drawImage(img, radiusX * imgSize, radiusY * imgSize, imgSize, imgSize);
    ctx.strokeText(username, radiusX * imgSize + imgSize/4, radiusY * imgSize + imgSize/4);
}

function DrawPlayers(names, positionsY, positionsX, msgs) {
    var div = document.getElementById("chat");

    var ctx = playerCanvas.getContext("2d");
    var img = document.getElementById("Player.png");

    ctx.clearRect(0, 0, playerCanvas.width, playerCanvas.height);
    ctx.font = "15px Arial";

    removeAllChildren(div);

    for (let i = 0; i < names.length; i++) {
        let y = imgSize * (positionsY[i] + radiusY - playerY);
        let x = imgSize * (positionsX[i] + radiusX - playerX);

        if (msgs[i] != "") {
            var textArea = document.createElement("textarea");
            textArea.rows = 4;
            textArea.cols = 30;
            textArea.className = "chat";
            textArea.disabled = true;
            textArea.innerHTML = msgs[i];

            textArea.style.top = y;
            textArea.style.left = x;

            div.appendChild(textArea);
        }

        // Too choppy to render the player here
        if (names[i] == username) {
            continue;
        }
        ctx.drawImage(img, x, y, imgSize, imgSize);
        ctx.strokeText(names[i], x + imgSize / 4, y + imgSize / 4);
    }
}

function DrawEntities(names, positionsY, positionsX) {
    var ctx = entityCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, entityCanvas.width, entityCanvas.height);

    for (let i = 0; i < names.length; i++) {
        let y = imgSize * (positionsY[i] + radiusY - playerY);
        let x = imgSize * (positionsX[i] + radiusX - playerX);

        img = document.getElementById(names[i] + ".png");

        ctx.drawImage(img, x, y, imgSize, imgSize);
    }
}

function DrawDirectionLine(targetY, targetX) {
    /*var ctx = directionCanvas.getContext("2d");

    ctx.clearRect(0, 0, directionCanvas.width, directionCanvas.height);
    ctx.beginPath();

    var centerX = (radiusX * imgSize) + imgSize / 2;
    var centerY = (radiusY * imgSize) + imgSize / 2;

    ctx.moveTo(centerX, centerY);
    ctx.lineTo(targetX, targetY);
    ctx.stroke();

    var Vx = targetX - centerX;
    var Vy = targetY - centerY;
    ctx.strokeText(Vx + ", " + Vy, radiusX * imgSize, radiusY * imgSize);
    ctx.strokeText(Math.atan2(Vy, Vx) * 180 / Math.PI, radiusX * imgSize, radiusY * imgSize);
    ctx.strokeText(Vy / Vx, radiusX * imgSize, radiusY * imgSize);*/

    var m = Math.floor((targetY - worldCanvasOriginY) / imgSize);
    var n = Math.floor((targetX - worldCanvasOriginX) / imgSize);
    worldTargetM = m + worldOriginM;
    worldTargetN = n + worldOriginN;

    mouseTargetY = targetY;
    mouseTargetX = targetX;

    //ctx.strokeText("[" + m + "][" + n + "]", targetX, targetY);
    //ctx.strokeText("x:" + targetX.toFixed(2) + " y:" + targetY.toFixed(2), targetX, targetY);
    //ctx.strokeRect(targetX - imgSize/2, targetY - imgSize/2, imgSize, imgSize);
}

function ChangeDirection(targetY, targetX) {
    var centerX = (radiusX * imgSize) + imgSize / 2;
    var centerY = (radiusY * imgSize) + imgSize / 2;
    var Vx = targetX - centerX;
    var Vy = targetY - centerY;

    var angleDeg = Math.atan2(Vy, Vx) * 180 / Math.PI;

    if (angleDeg > 0) {
        if (angleDeg <= 45) {
            playerDirection = "East";
        } else if (angleDeg <= 135) {
            playerDirection = "South";
        } else if (angleDeg > 135) {
            playerDirection = "West";
        }
    } else {
        if (angleDeg >= -45) {
            playerDirection = "East";
        } else if (angleDeg >= -135) {
            playerDirection = "North";
        } else if (angleDeg < -135) {
            playerDirection = "West";
        }
    }

    connection.invoke("GetDirection", playerDirection).catch(function (err) {
        return console.error(err.toString());
    });
}

function DrawCanvases(pos1y, pos1x, pos2y, pos2x, images) {
    var ctx = canvasCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, canvasCanvas.width, canvasCanvas.height);

    for (let i = 0; i < images.length; i++) {
        if (images[i] == "0") {
            continue;
        }
        
        var y = imgSize * (pos1y[i] + radiusY - playerY);
        var x = imgSize * (pos1x[i] + radiusX - playerX);

        var scaleY = pos2y[i] - pos1y[i] + 1;
        var scaleX = pos2x[i] - pos1x[i] + 1;

        img = document.getElementById(images[i]);

        ctx.drawImage(img, x, y, imgSize * scaleX, imgSize * scaleY);
    }
}

function OpenChat() {
    isTyping = true;
    chatInput.hidden = false;
    chatInput.value = "";
    chatInput.focus();
}

function SendMessage() {
    var msg = chatInput.value;
    connection.invoke("SendMessage", username, msg).catch(function (err) {
        return console.error(err.toString());
    });
    chatInput.hidden = true;
    isTyping = false;
}

connection.start().then(function () {

});

connection.on("GetMessages", function (names, msgs) {
    var div = document.getElementById("chat");
    var textArea = document.createElement("textarea");

    textArea.rows = 4;
    textArea.cols = 30;
    textArea.className = "chat";
    textArea.disabled = true;

    textArea.innerHTML = msg;

    if (from == username) {
        textArea.style.top = radiusY * imgSize;
        textArea.style.left = radiusX * imgSize;
    } else {

    }

    div.appendChild(textArea);
});

connection.on("Debug", function (msg) {
    console.log(msg);
});

connection.on("GetWorld", function (world, numLayers, worldRows, worldCols) {
    DrawWorld(world, numLayers, worldRows, worldCols, playerX, playerY);
});

connection.on("GetPlayers", function (playerNames, positionsY, positionsX, msgs) {
    DrawPlayers(playerNames, positionsY, positionsX, msgs);
});

connection.on("GetEntities", function (entityNames, positionsY, positionsX) {
    DrawEntities(entityNames, positionsY, positionsX);
});

connection.on("GetCanvasPositions", function (pos1y, pos1x, pos2y, pos2x, images) {
    var imagesLoaded = 0;
    var imgSection = document.getElementById("imgs");
    //removeAllChildren(imgSection);

    if (images.length == 0) {
        DrawCanvases(pos1y, pos1x, pos2y, pos2x, images);
    }

    for (let i = 0; i < images.length; i++) {
        if (images[i] == "0") {
            imagesLoaded++;
            if (imagesLoaded == images.length) {
                DrawCanvases(pos1y, pos1x, pos2y, pos2x, images);
            }
            continue;
        }

        var img = document.getElementById(images[i]);

        if (img == null) {
            img = document.createElement("img");
            img.id = images[i];
            img.hidden = true;
            img.src = "/images/canvases/" + images[i];
            imgSection.appendChild(img);
        } else {
            img.src = "/images/canvases/" + images[i];
        }

        img.onload = function () {
            imagesLoaded++;
            if (imagesLoaded == images.length) {
                DrawCanvases(pos1y, pos1x, pos2y, pos2x, images);
            }
        }
    }
});

connection.on("GetPosition", function (newX, newY) {
    playerX = newX;
    playerY = newY;
});

connection.on("GetDirection", function (dir) {
    playerDirection = dir;
});

connection.on("GetInventory", function (quantities, items, positions) {
    var i;

    for (i = 0; i < inventoryItemAmounts.length; i++) {
        inventoryItemAmounts[i] = 0;
        inventoryItemNames[i] = "BlankItem";
    }

    for (i = 0; i < positions.length; i++) {
        inventoryItemAmounts[positions[i]] = quantities[i];
        inventoryItemNames[positions[i]] = items[i];
    }

    LoadInventory();
});

connection.on("GetHotbar", function (quantities, items, positions) {
    var i;

    for (i = 0; i < hotbarItemAmounts.length; i++) {
        hotbarItemAmounts[i] = 0;
        hotbarItemNames[i] = "BlankItem";
    }

    for (i = 0; i < positions.length; i++) {
        hotbarItemAmounts[positions[i]] = quantities[i];
        hotbarItemNames[positions[i]] = items[i];
    }

    LoadHotbar();
});

connection.on("GetTileInUse", function (type, data) {
    if (stoppedUsingTile) {
        return;
    }

    if (type == "Canvas") {
        drawingDiv.hidden = false;
        isUsingTile = true;
        stoppedUsingTile = false;
    }
});

connection.on("StopUsingTile", function () {

});

connection.on("GetResponse", function (type, quantities, items) {

});

var map = {};
onkeydown = onkeyup = function (e) {
    e = e || this.event;
    map[e.keyCode] = e.type == 'keydown';
}

window.setInterval(CheckKeys, 10);

function CheckKeys() {
    var Vx = 0;
    var Vy = 0;

    if (isTyping) {
        if (map[13]) {
            SendMessage();
        }
        return;
    }

    if (map[87]) { // w
        Vy = -1;
    }
    if (map[65]) { // a
        Vx = -1;
    }
    if (map[83]) { // s
        Vy = 1;
    }
    if (map[68]) { // d
        Vx = 1;
    }

     // 1 - 5
    if (map[49]) {
        SelectHotbar(0);
    } else if (map[50]) {
        SelectHotbar(1);
    } else if (map[51]) {
        SelectHotbar(2);
    } else if (map[52]) {
        SelectHotbar(3);
    } else if (map[53]) {
        SelectHotbar(4);
    }

    if (map[13]) { // Enter
        OpenChat();

    } else if (map[69]) { // e
        inventoryTable.hidden = false;
        canMove = false;

    } else if (map[70]) { // f
        UseItem(hotbarItemNames[hotbarPosition], hotbarPosition, "Use");

    } else if (map[81]) { // q
        if (isUsingTile) {
            StopUsingTile();
        } else if (inventoryTable.hidden == false) {
            inventoryTable.hidden = true;
            canMove = true;
        } else {
            //UseItem(hotbarItemNames[hotbarPosition], hotbarPosition, "Drop");
        }

    } else if (map[88]) { // x
        UseTile();
    }

    if (canMove) {
        connection.invoke("GetMovementInput", Vx, Vy).catch(function (err) {
            return console.error(err.toString());
        });
    }
}

document.addEventListener("wheel", function (event) {
    if (event.deltaY > 0) {
        var res1 = modulo(hotbarPosition + 1, 5);
        SelectHotbar(res1);
    } else if (event.deltaY < 0) {
        var res2 = modulo(hotbarPosition - 1, 5);
        SelectHotbar(res2);
    }
});

document.addEventListener("mousemove", function (event) {
    var rect = event.target.getBoundingClientRect();
    DrawDirectionLine(event.clientY - rect.top, event.clientX - rect.left);
    ChangeDirection(event.clientY - rect.top, event.clientX - rect.left);
});

document.addEventListener("mousedown", function (event) {
    if (!isDrawing && inventoryTable.hidden) {
        UseItem(hotbarItemNames[hotbarPosition], hotbarPosition, "Use");
    }
});

drawingCanvas.addEventListener("mousedown", function (event) {
    var rect = event.target.getBoundingClientRect();
    lastY = event.clientY - rect.top;
    lastX = event.clientX - rect.left;
    isDrawing = true;
});

drawingCanvas.addEventListener("mouseup", function (event) {
    var imageData = drawingCanvas.toDataURL("image/png");
    imageData = imageData.replace('data:image/png;base64,', '');

    connection.invoke("GetDrawingData", imageData, tileInUseM, tileInUseN).catch(function (err) {
        return console.error(err.toString());
    });
    isDrawing = false;
});

drawingCanvas.addEventListener("mousemove", function (event) {
    if (isDrawing) {
        var ctx = drawingCanvas.getContext("2d");
        var rect = event.target.getBoundingClientRect();
        ctx.lineCap = "round";
        ctx.lineWidth = 3;
        ctx.strokeStyle = "#ffffff";
        ctx.beginPath();
        ctx.moveTo(lastX, lastY);
        ctx.lineTo(event.clientX - rect.left, event.clientY - rect.top);
        ctx.stroke();

        lastY = event.clientY - rect.top;
        lastX = event.clientX - rect.left;
    }
});

closeDrawingCanvas.addEventListener("click", function (event) {
    StopUsingTile();
});

clearDrawingCanvas.addEventListener("click", function (event) {
    var ctx = drawingCanvas.getContext("2d");
    ctx.clearRect(0, 0, drawingCanvas.width, drawingCanvas.height);
    ctx.fillStyle = "black";
    ctx.beginPath();
    ctx.fillRect(0, 0, drawingCanvas.width, drawingCanvas.height);

    var imageData = drawingCanvas.toDataURL("image/png");
    imageData = imageData.replace('data:image/png;base64,', '');

    connection.invoke("GetDrawingData", imageData, worldTargetM, worldTargetN).catch(function (err) {
        return console.error(err.toString());
    });
    isDrawing = false;
});

slider.oninput = function () {
    imgSize = this.value * 50;
    RefreshCanvasVariables();
}

menuBtn.addEventListener("click", function (event) {

});

chatBtn.addEventListener("click", function (event) {

});

inventoryBtn.onclick = function () {
    inventoryTable.hidden = !inventoryTable.hidden;
    canMove = !canMove;
}

window.onresize = function () {
    PositionUI();
    RefreshCanvasVariables();
}