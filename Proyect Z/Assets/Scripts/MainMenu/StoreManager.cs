using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreManager : MonoBehaviour
{
    [Header("Monedas del jugador")]
    public int monedasZ = 0;

    [Header("Precios de los trajes")]
    public int pistolaPlata = 1000;
    public int pistolaOro = 2000;
    public int escopetaPlata = 5000;
    public int escopetaOro = 8000;

    public bool pistolaPlataComprada = false;
    public bool pistolaOroComprada = false;
    public bool escopetaPlataComprada = false;
    public bool escopetaOroComprada = false;

    public int precioSeleccionado = 0;
    private string armaSeleccionada = "";

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

    [Header("Imagen de preview")]
    public Image imagenPreview;
    public Sprite pistolaPlataSprite;
    public Sprite pistolaOroSprite;
    public Sprite escopetaPlataSprite;
    public Sprite escopetaOroSprite;

    void Start()
    {
        monedasZ = PlayerPrefs.GetInt("MonedasZ", 0);

        pistolaPlataComprada = PlayerPrefs.GetInt("Arma_PistolaPlata", 0) == 1;
        pistolaOroComprada = PlayerPrefs.GetInt("Arma_PistolaOro", 0) == 1;
        escopetaPlataComprada = PlayerPrefs.GetInt("Arma_EscopetaPlata", 0) == 1;
        escopetaOroComprada = PlayerPrefs.GetInt("Arma_EscopetaOro", 0) == 1;
    }    

    public void SeleccionarArma(string nombreArma)
    {
        armaSeleccionada = nombreArma;

        switch (nombreArma)
        {
            case "PistolaPlata":
                if (pistolaPlataComprada)
                {
                    textoMonedas.text = "Comprado";
                    imagenPreview.sprite = pistolaPlataSprite;
                    return;
                }                
                precioSeleccionado = pistolaPlata;
                imagenPreview.sprite = pistolaPlataSprite;
                break;
            case "PistolaOro":
                if (pistolaOroComprada)
                {
                    textoMonedas.text = "Comprado";
                    imagenPreview.sprite = pistolaOroSprite;
                    return;
                }
                precioSeleccionado = pistolaOro;
                imagenPreview.sprite = pistolaOroSprite;
                break;
            case "EscopetaPlata":
                if (escopetaPlataComprada)
                {
                    textoMonedas.text = "Comprado";
                    imagenPreview.sprite = escopetaPlataSprite;
                    return;
                }
                precioSeleccionado = escopetaPlata;
                imagenPreview.sprite = escopetaPlataSprite;
                break;
            case "EscopetaOro":
                if (escopetaOroComprada)
                {
                    textoMonedas.text = "Comprado";
                    imagenPreview.sprite = escopetaOroSprite;
                    return;
                }
                precioSeleccionado = escopetaOro;
                imagenPreview.sprite = escopetaOroSprite;
                break;
        }
        textoMonedas.text = $"{precioSeleccionado} Monedas Z";
    }

    public void ComprarArma()
    {
        if (precioSeleccionado == 0)
        {
            Debug.Log("No se ha seleccionado un arma");
            return;
        }

        switch (armaSeleccionada)
        {
            case "PistolaPlata": if (pistolaPlataComprada) return; break;
            case "PistolaOro": if (pistolaOroComprada) return; break;
            case "EscopetaPlata": if (escopetaPlataComprada) return; break;
            case "EscopetaOro": if (escopetaOroComprada) return; break;
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

        switch (armaSeleccionada)
        {
            case "PistolaPlata":
                pistolaPlataComprada = true;
                PlayerPrefs.SetInt("Arma_PistolaPlata", 1);
                break;
            case "PistolaOro":
                pistolaOroComprada = true;
                PlayerPrefs.SetInt("Arma_PistolaOro", 1);
                break;
            case "EscopetaPlata":
                escopetaPlataComprada = true;
                PlayerPrefs.SetInt("Arma_EscopetaPlata", 1);
                break;
            case "EscopetaOro":
                escopetaOroComprada = true;
                PlayerPrefs.SetInt("Arma_EscopetaOro", 1);
                break;
        }

        PlayerPrefs.SetInt("MonedasZ", monedasZ);
        PlayerPrefs.Save();

        textoMonedas.text = "Comprado";
        CerrarConfirmar();
    }

    public void AbrirPanelMonedas()
    {
        panelSinMonedas.SetActive(false);
        panelTienda.SetActive(false);
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
        textoSaldo.text = $"Precio: {saldoSeleccionado}€";
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
