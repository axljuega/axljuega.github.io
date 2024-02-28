// script.js

document.addEventListener('DOMContentLoaded', function() {
    // Función para cargar texto del botón desde my_data2.txt al cargar la página
    fetch('data/my_data2.txt')
    .then(response => response.text())
    .then(texto => {
        document.getElementById('btnSumar').textContent = texto;
    })
    .catch(error => console.error('Error al cargar el texto inicial del botón:', error));

    document.getElementById('btnSumar').addEventListener('click', function() {
        // Verificar si el botón ya ha sido presionado para sumar
        if (this.textContent === "Sumar") {
            sumar(); // Función para realizar la suma
        } else {
            // Cambiar texto del botón a "Sumar" después de la primera presión
            this.textContent = "Sumar";
            // Mostrar el resultado con estilo después del primer clic
            document.getElementById('resultado').style.visibility = "visible";
            document.getElementById('resultado').classList.add('resultado-estilizado');
        }
    });
});

function sumar() {
    var num1 = document.getElementById('numero1').value;
    var num2 = document.getElementById('numero2').value;
    var suma = parseInt(num1) + parseInt(num2);
    document.getElementById('resultado').innerText = "Resultado: " + suma;
}
