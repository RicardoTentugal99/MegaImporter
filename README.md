# MegaImporter

Utilitário Windows para importar listas de **números de série** e enviá-los, linha a linha,
para uma aplicação externa (tipicamente o **Primavera**) através de simulação de teclado.

Evita a picagem manual campo a campo: colas a lista, confirmas, e a aplicação escreve tudo por ti.

---

## Funcionalidades

- **Importar do clipboard** — cola uma lista copiada de Excel, Outlook, web ou de um ficheiro de texto.
- **Normalização automática de espaçamento** — o principal problema de colar listas de SN:
  - remove espaços e tabs antes/depois de cada serial;
  - aceita vários seriais na mesma linha, separados por espaços;
  - ignora linhas em branco;
  - limpa espaços duros (`NBSP`) e de largura zero — o lixo invisível típico de copiar do Excel/Outlook,
    que faz o serial entrar errado no destino;
  - aceita quebras de linha `CRLF` (Windows) e `LF` (web/Linux).
- **Modo scanner** — caixa de picagem dedicada; o leitor pica o SN e envia Enter, a linha é acrescentada à lista.
- **Deteção de duplicados** na importação, com opção de os remover.
- **Envio com contagem decrescente** de 4 segundos para colocares o cursor no campo de destino.
- **Botão PARAR** — durante o envio o botão Enviar transforma-se em PARAR; volta à janela e interrompes a meio.
- **Escape de caracteres especiais** — seriais com `+ ^ % ~ ( ) { } [ ]` são escapados antes do envio,
  para não dispararem atalhos de teclado na aplicação de destino.

---

## Requisitos

- Windows 10 / 11
- O executável da release é **self-contained**: não é preciso instalar o .NET.
- Para compilar a partir do código: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Como usar

1. Copia a lista de números de série (Ctrl+C).
2. Abre o MegaImporter e clica em **Importar**.
3. Confere o contador de itens carregados.
4. Coloca o cursor no campo certo da aplicação de destino.
5. Clica em **Enviar** e tens 4 segundos para focar esse campo.
6. Para interromper, volta ao MegaImporter e clica em **PARAR**.

Em alternativa ao passo 1–2, usa a caixa **SCANNER - PICAGEM** com um leitor de código de barras.

---

## Configuração

As constantes no topo de [`Form1.vb`](Form1.vb) controlam o comportamento do envio:

```vb
' Pausa entre cada tecla enviada (ms). Sobe para 60-80 se a aplicação de destino perder caracteres.
Private Const PausaEntreSeriais As Integer = 40

' Segundos de contagem decrescente antes de começar a enviar.
Private Const SegundosParaFocar As Integer = 4
```

Se a aplicação de destino avançar de campo com **Tab** em vez de **Enter**, troca a linha do envio:

```vb
SendKeys.SendWait("{ENTER}")   ' -> "{TAB}"
```

---

## Compilar

```
dotnet build -c Release
```

Executável self-contained num único ficheiro:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Aviso

O envio é feito com `SendKeys`, ou seja, **simulação de teclado**: as teclas vão para a janela que estiver
em primeiro plano. Se mudares de janela a meio do envio, o texto é escrito onde não devia.
Usa o botão PARAR se isso acontecer.
