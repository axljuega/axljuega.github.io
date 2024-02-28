// script.js
function sumar() {
    var num1 = document.getElementById('numero1').value;
    var num2 = document.getElementById('numero2').value;
    var suma = parseInt(num1) + parseInt(num2);
    document.getElementById('resultado').innerText = "Resultado: " + suma;
}
