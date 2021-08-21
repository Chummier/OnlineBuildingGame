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

var maxSpritesX = Math.floor(gameCanvas.width / imgSize);
var maxSpritesY = Math.floor(gameCanvas.height / imgSize);
var radiusX = Math.floor(maxSpritesX / 2);
var radiusY = Math.floor(maxSpritesY / 2);

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
var username = document.getElementById("Username").value;
var canMove = true;
var playerDirection = "South";

inventoryTable.hidden = true;
hotbarTable.hidden = false;

PositionUI();
SelectHotbar(hotbarPosition);

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

function UseItem(itemName, index, action) {
    var ctx = entityCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, entityCanvas.width, entityCanvas.height);

    var itemX = radiusX * imgSize; 
    var itemY = radiusY * imgSize; // set x and y to player position
    var worldTargetM = playerY;
    var worldTargetN = playerX;

    if (playerDirection == "North") {
        itemY -= imgSize;
        worldTargetM--;
    } else if (playerDirection == "South") {
        itemY += imgSize;
        worldTargetM++;
    } else if (playerDirection == "East") {
        itemX += imgSize;
        worldTargetN++;
    } else if (playerDirection == "West") {
        itemX -= imgSize;
        worldTargetN--;
    }

    img = document.getElementById(itemName + ".png");
    ctx.drawImage(img, itemX, itemY, imgSize, imgSize);

    connection.invoke("GetItemUse", action, itemName, index, worldTargetM, worldTargetN).catch(function (err) {
        return console.error(err.toString());
    });
}

function PositionUI() {
    var canvasCoords = gameCanvas.getBoundingClientRect();

    inventoryTable.style.top = canvasCoords.top;
    inventoryTable.style.left = canvasCoords.left;

    hotbarTable.style.top = canvasCoords.bottom;
    hotbarTable.style.left = canvasCoords.right;

    chatInput.style.left = imgSize * radiusX;
    chatInput.style.top = imgSize * radiusY;
}

function DrawWorld(world, numLayers, worldRows, worldCols, centerX, centerY) {
    var ctx = gameCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, gameCanvas.width, gameCanvas.height);
    ctx.font = "15px Arial";

    var m = Math.ceil(centerY) - radiusY;
    var n = Math.ceil(centerX) - radiusX;
    for (let y = 0; y < gameCanvas.height; y += imgSize) {
        for (let x = 0; x < gameCanvas.width; x += imgSize) {
            if (m < 0 || n < 0) {
                img = document.getElementById("Air.png");
                ctx.drawImage(img, x, y, imgSize, imgSize);
            } else if (m >= worldRows || n >= worldCols) {
                img = document.getElementById("Air.png");
                ctx.drawImage(img, x, y, imgSize, imgSize);
            } else {
                for (let l = 0; l < numLayers; l++) {
                    img = document.getElementById(world[l][m][n]);
                    ctx.drawImage(img, x, y, imgSize, imgSize);
                }
            }
            n++;
        }
        n = Math.ceil(centerX) - radiusX;
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

connection.on("GetPosition", function (newX, newY) {
    playerX = newX;
    playerY = newY;
});

connection.on("GetDirection", function (dir) {
    playerDirection = dir;
});

connection.on("GetInventory", function (quantities, items) {
    var i;
    for (i = 0; i < quantities.length; i++) {
        inventoryItemAmounts[i] = quantities[i];
        inventoryItemNames[i] = items[i];
    }
    for (i = i; i < inventoryItemAmounts.length; i++) {
        inventoryItemAmounts[i] = 0;
        inventoryItemNames[i] = "BLANK";
    }
    LoadInventory();
});

connection.on("GetHotbar", function (quantities, items) {
    var i;
    for (i = 0; i < quantities.length; i++) {
        hotbarItemAmounts[i] = quantities[i];
        hotbarItemNames[i] = items[i];
    }
    for (i = i; i < hotbarItemAmounts.length; i++) {
        hotbarItemAmounts[i] = 0;
        hotbarItemNames[i] = "BLANK";
    }
    LoadHotbar();
});

connection.on("GetResponse", function (type, quantities, items) {

});

document.addEventListener("keydown", function (event) {
    var Vx = 0;
    var Vy = 0;
    if (event.key == "ArrowUp") {
        Vy = -1;
    } else if (event.key == "ArrowLeft") {
        Vx = -1;
    } else if (event.key == "ArrowDown") {
        Vy = 1;
    } else if (event.key == "ArrowRight") {
        Vx = 1;

        // 
    } else if (event.key == "z") {

        // open inventory
    } else if (event.key == "e") {
        if (!isTyping) {
            inventoryTable.hidden = false;
            canMove = false;
        }
        
        // close menu, cancel, drop item
    } else if (event.key == "q") {
        if (isTyping) {
            chatInput.hidden = true;
            isTyping = false;
            return;
        }

        if (inventoryTable.hidden) {
            UseItem(hotbarItemNames[hotbarPosition], hotbarPosition, "Drop");
        }
        inventoryTable.hidden = true;
        canMove = true;

        

        // use tool, attack
    } else if (event.key == "f") {
        if (!isTyping) {
            UseItem(hotbarItemNames[hotbarPosition], hotbarPosition, "Use");
        }
        
    } else if (event.key == "1") {
        SelectHotbar(0);
    } else if (event.key == "2") {
        SelectHotbar(1);
    } else if (event.key == "3") {
        SelectHotbar(2);
    } else if (event.key == "4") {
        SelectHotbar(3);
    } else if (event.key == "5") {
        SelectHotbar(4);
    }

    else if (event.key == "Enter") {
        if (isTyping) {
            SendMessage();
            chatInput.hidden = true;
            isTyping = false;
        } else {
            OpenChat();
        }
    }

    if (canMove) {
        connection.invoke("GetMovementInput", Vx, Vy).catch(function (err) {
            return console.error(err.toString());
        });
    }
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