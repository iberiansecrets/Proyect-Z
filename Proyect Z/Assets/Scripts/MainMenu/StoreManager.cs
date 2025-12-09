using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreManager : MonoBehaviour
{
    [Header("Monedas del jugador")]
    public int monedasZ = 0;

    [Header("Precios de los trajes")]
    public int personajeRojo = 1000;
    public int personajeAzul = 2000;
    public int personajeRosa = 5000;

    public bool rojoComprado = false;
    public bool azulComprado = false;
    public bool rosaComprado = false;

    public int precioSeleccionado = 0;
    private string trajeSeleccionado = "";

    [Header("Precios de las recargas")]
    public int monedas500 = 500;
    public int monedas1200 = 1300;
    public int monedas2500 = 2800;
    public int monedas5000 = 6000;

    [Header("Precios de las recargas")]
    public double pack500 = 1.50;
    public double pack1200 = 3.00;
    public double pack2500 = 6.00;
    public double pack5000 = 10.00;

    public double saldoSeleccionado = 0.0;
    private string packSeleccionado = "";

    [Header("Paneles de la tienda")]
    public GameObject panelTienda;
    public GameObject panelConfirmar;
    public GameObject panelSinMonedas;
    public GameObject panelMonedas;

    [Header("Textos")]
    public TMP_Text textoConfirmar;
    public TMP_Text textoMonedas;
    public TMP_Text textoSinMonedas;
    public TMP_Text textoSaldo;

    void Start()
    {
        monedasZ = PlayerPrefs.GetInt("MonedasZ", 0);

        rojoComprado = PlayerPrefs.GetInt("Traje_Rojo", 0) == 1;
        azulComprado = PlayerPrefs.GetInt("Traje_Azul", 0) == 1;
        rosaComprado = PlayerPrefs.GetInt("Traje_Rosa", 0) == 1;
    }    

    public void SeleccionarTraje(string nombreTraje)
    {
        trajeSeleccionado = nombreTraje;

        switch (nombreTraje)
        {
            case "Rojo":
                if (rojoComprado)
                {
                    textoMonedas.text = "Comprado";
                    return;
                }                
                precioSeleccionado = personajeRojo; 
                break;
            case "Azul":
                if (azulComprado)
                {
                    textoMonedas.text = "Comprado";
                    return;
                }
                precioSeleccionado = personajeAzul;
                break;
            case "Rosa":
                if (rosaComprado)
                {
                    textoMonedas.text = "Comprado";
                    return;
                }
                precioSeleccionado = personajeRosa;
                break;
        }
        textoMonedas.text = $"{precioSeleccionado} monedas";
    }

    public void ComprarTraje()
    {
        if (precioSeleccionado == 0)
        {
            Debug.Log("No se ha seleccionado traje");
            return;
        }

        switch (trajeSeleccionado)
        {
            case "Rojo": if (rojoComprado) return; ; break;
            case "Azul": if (azulComprado) return; ; break;
            case "Rosa": if (rosaComprado) return; ; break;
        }

        if (monedasZ >= precioSeleccionado)
        {
            panelConfirmar.SetActive(true);
            textoConfirmar.text = $"Tienes {monedasZ} monedas Z.";
        }
        else
        {
            panelSinMonedas.SetActive(true);
            textoSinMonedas.text = $"Te faltan {precioSeleccionado - monedasZ} monedas Z.";
        }
    }

    public void ConfirmarCompra()
    {
        monedasZ -= precioSeleccionado;

        switch (trajeSeleccionado)
        {
            case "Rojo":
                rojoComprado = true;
                PlayerPrefs.SetInt("Traje_Rojo", 1);
                break;

            case "Azul":
                azulComprado = true;
                PlayerPrefs.SetInt("Traje_Azul", 1);
                break;

            case "Rosa":
                rosaComprado = true;
                PlayerPrefs.SetInt("Traje_Rosa", 1);
                break;
        }

        PlayerPrefs.SetInt("MonedasZ", monedasZ);
        PlayerPrefs.SetInt("Traje_" + trajeSeleccionado, 1);
        PlayerPrefs.Save();

        Debug.Log("Se ha comprado el traje " + trajeSeleccionado);
        CerrarConfirmar();
    }

    public void AbrirPanelMonedas()
    {
        panelSinMonedas.SetActive(false);
        panelMonedas.SetActive(true);
    }

    public void SeleccionarMonedas(string pack)
    {
        packSeleccionado = pack;

        switch (pack)
        {
            case "500": saldoSeleccionado = pack500; break;
            case "1200": saldoSeleccionado = pack1200; break;
            case "2500": saldoSeleccionado = pack2500; break;
            case "5000": saldoSeleccionado = pack5000; break;
        }
        textoSaldo.text = $"{saldoSeleccionado}€";
    }   

    public void ComprarPackMonedas()
    {
        int monedasARecibir = 0;

        switch (packSeleccionado)
        {
            case "500": monedasARecibir = monedas500; break;
            case "1200": monedasARecibir = monedas1200; break;
            case "2500": monedasARecibir = monedas2500; break;
            case "5000": monedasARecibir = monedas5000; break;
            default:
                Debug.LogWarning("No se ha seleccionado ningún pack.");
                return;
        }

        
        monedasZ += monedasARecibir;
        PlayerPrefs.SetInt("MonedasZ", monedasZ);
        PlayerPrefs.Save();

        Debug.Log($"Has comprado {monedasARecibir} monedas Z.");

        CerrarPanelMonedas();
    }

    public void CerrarConfirmar()
    {
        panelConfirmar.SetActive(false);
    }

    public void CerrarSinMonedas()
    {
        panelSinMonedas.SetActive(false);
    }

    public void CerrarPanelMonedas()
    {
        panelMonedas.SetActive(false);
        panelTienda.SetActive(true);
    }
}
