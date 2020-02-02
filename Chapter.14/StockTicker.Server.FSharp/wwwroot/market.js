var socket;
var stockMarket = {};
var portfolio = {};
var orders = {};

var stockRows = document.getElementById('stockRows');
var portfolioRows = document.getElementById('portfolioRows');
var orderRows = document.getElementById('orderRows');
var orderSymbol = document.getElementById('order_symbol');
var orderPrice = document.getElementById('order_price');
var orderQuantity = document.getElementById('order_quantity');
var orderSubmit = document.getElementById('order_submit');
var orderSellButton = document.getElementById('ordertype_sell');
var orderBuyButton = document.getElementById('ordertype_buy');
var cash = parseFloat(document.getElementById('user-cash').value);
var cashMarker = document.getElementById('cash');
var userId = document.getElementById('user-id').value;
cashMarker.textContent = "$" + cash.toFixed(2);


function openWebSocket() {
    var loc = window.location, new_uri;
    if (loc.protocol === "https:") {
        new_uri = "wss:";
    } else {
        new_uri = "ws:";
    }
    new_uri += "//" + loc.host + ":" + loc.port;
    new_uri += "/ws?userId=" + userId;
    socket = new WebSocket(new_uri);
    
    console.log('openWebSocket userid ' + userId);
    
    socket.onopen = function () {
        console.log('INFO: WebSocket opened successfully');
    };

    socket.onclose = function () {
        console.log('INFO: WebSocket closed');
        openWebSocket();
    };
    
    socket.onmessage = function (messageEvent) {
        var update = JSON.parse(messageEvent.data);
        if(update.Kind == 'stock') {
            stockMarket[update.Symbol] = update;
            renderStockMarket();

            if (update.Symbol in portfolio) {
                portfolio[update.Symbol].Price = update.Price;
                renderPortfolio();
            }

        } else if (update.Kind == 'portfolio') {
            if (update.Command == 'delete') {
                delete portfolio[update.id];
            }
            else {
                portfolio[update.Symbol] = update;
            }
            renderPortfolio();
        }
        else if (update.Kind == 'order') {
            if (update.Command == 'delete') {
                delete orders[update.OrderId];
            }
            else {
                orders[update.OrderId] = update;
            }
            renderOrders();
        } else if (update.Kind == 'cash') {
            cash = update.Cash;
            cashMarker.textContent = "$" + cash.toFixed(2);
            orders = {};
            renderOrders();
            portfolio = {};
            renderPortfolio();

        } else {
            console.log("Unknown message: " + messageEvent.data);
        }

    }
}
openWebSocket();

function setValue(el, v) {
    el.value = v;
    el.oninput();
}

function renderStockMarket() {
    var sorted = [];
    for(var key in stockMarket) {
        sorted[sorted.length] = key;
    }
    sorted.sort();
    stockRows.style.visibility = "hidden";
    clearChildren(stockRows);
    for(var symbol in sorted) {
        var stock = stockMarket[sorted[symbol]];
        var stockColor = stock.Price > stock.DayOpen ? "has-text-success" : stock.Price < stock.DayOpen ? "has-text-danger" : "";
        var changeColor = stock.LastChange > 0 ? "has-text-success" : stock.LastChange < 0 ? "has-text-danger" : "";
        var html = `<tr>
          <th><a href="#" onclick="return selectSymbol(this.text, event);">${stock.Symbol}</a></th>
          <td class="has-text-right ${stockColor}">${stock.Price.toFixed(2)}</td>
          <td class="has-text-right">${stock.DayOpen.toFixed(2)}</td>
          <td class="has-text-right">${stock.DayHigh.toFixed(2)}</td>
          <td class="has-text-right">${stock.DayLow.toFixed(2)}</td>
          <td class="has-text-right ${changeColor}">${stock.LastChange.toFixed(2)}</td>
        </tr>`;
        stockRows.insertAdjacentHTML( 'beforeend', html );
    }
    stockRows.style.visibility = "";
}

function renderPortfolio() {
    var sorted = [];
    for(var key in portfolio) {
        sorted[sorted.length] = key;
    }
    sorted.sort();
    portfolioRows.style.visibility = "hidden";
    clearChildren(portfolioRows);
    for(var symbol in sorted) {
        var stock = portfolio[sorted[symbol]];
        var html = `<tr>
          <th><a href="#" onclick="return selectSymbol(this.text, event);">${stock.Symbol}</a></th>
          <td class="has-text-right">${stock.Price.toFixed(2)}</td>
          <td class="has-text-right" >${stock.Quantity}</td>
          <td class="has-text-right" >${(stock.Price * stock.Quantity).toFixed(2)}</td>
        </tr>`;
        portfolioRows.insertAdjacentHTML( 'beforeend', html );
    }
    portfolioRows.style.visibility = "";
}

function renderOrders() {
    var sorted = [];
    for(var key in orders) {
        sorted[sorted.length] = key;
    }
    sorted.sort();
    orderRows.style.visibility = "hidden";
    clearChildren(orderRows);
    for(var idx in sorted) {
        var order = orders[sorted[idx]];
        var orderTypeString = order.Type == 'buy' ? "<td class='has-text-success'>Buy</td>" : "<td class='has-text-danger'>Sell</td>";
        var html = `<tr>
            ${orderTypeString}
            <td><a href="#" onclick="return selectSymbol(this.text, event);">${order.Symbol}</a></td>
            <td class="has-text-right">${order.Price.toFixed(2)}</td>
            <td class="has-text-right">${order.Quantity.toFixed(2)}</td>
            <td class="has-text-right">${(order.Price * order.Quantity).toFixed(2)}</td>
          </tr>`;
        orderRows.insertAdjacentHTML( 'beforeend', html );
    }
    orderRows.style.visibility = "";
}
function clearChildren(el) {
    while (el.firstChild) {
        el.removeChild(el.firstChild);
    }
}

function selectSymbol(symbol, e) {
    e.preventDefault();
    setValue(orderSymbol, symbol);
    orderPrice.focus();

    return false;
}

function placeOrder(e) {
    e.preventDefault();
    var order = {
        userId: userId,
        symbol: orderSymbol.value,
        quantity: orderQuantity.value,
        type: orderBuyButton.classList.contains('is-selected') ? 'buy' : 'sell',
        price: orderPrice.value
    };
    postData('/placeorder', order)
        .then(data => {
    orderSymbol.value = '';
    orderPrice.value = '';
    orderQuantity.value = '';
    updateOrderForm();
})
.catch(error => console.error(error));
    return false;
}

function updateOrderForm() {
    orderSubmit.toggleAttribute('disabled',
        orderSymbol.value == '' ||
        orderPrice.value == '' ||
        orderQuantity.value == '');
}

function updateOrderType(e) {
    var isBuy = e == orderBuyButton;
    orderBuyButton.classList.toggle('is-success', isBuy);
    orderBuyButton.classList.toggle('is-selected', isBuy);
    orderSellButton.classList.toggle('is-danger', !isBuy);
    orderSellButton.classList.toggle('is-selected', !isBuy);
}

function postData(url = '', data = {}) {
    return fetch(url, {
        method: 'POST', // *GET, POST, PUT, DELETE, etc.
        mode: 'cors', // no-cors, cors, *same-origin
        cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
        credentials: 'same-origin', // include, *same-origin, omit
        headers: {
            'Content-Type': 'application/json',
            // 'Content-Type': 'application/x-www-form-urlencoded',
        },
        redirect: 'follow', // manual, *follow, error
        referrer: 'no-referrer', // no-referrer, *client
        body: JSON.stringify(data), // body data type must match "Content-Type" header
    })
        .then(response => response.json()); // parses JSON response into native JavaScript objects 
}

updateOrderForm();
