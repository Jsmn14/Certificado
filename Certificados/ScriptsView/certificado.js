
var filePlanoFundo;

var textoNegrito = false;
var textoItalico = false;
var tamanhoFonte = 50;

function mudarTamanhoTexto() {
    tamanhoFonte = $("#tamanhoFonte").val();
    $(".caixatexto").css("font-size", tamanhoFonte +"px");
}

function aplicarNegritoTexto() {
    textoNegrito = !textoNegrito;
    if (textoNegrito) {
        $(".caixatexto").css("font-weight", "bold");
        $("#botao-negrito").css("box-shadow", "0 0 0 3px #c1c1c1");
    } else {
        $(".caixatexto").css("font-weight", "normal");
        $("#botao-negrito").css("box-shadow", "none");
    }
    
}

function aplicarItalicoTexto() {
    textoItalico = !textoItalico;
    if (textoItalico) {
        $(".caixatexto").css("font-style", "italic");
        $("#botao-italico").css("box-shadow", "0 0 0 3px #c1c1c1");
    } else {
        $(".caixatexto").css("font-style", "normal");
        $("#botao-italico").css("box-shadow", "none");
    }
}

function enviarCertificados() {
    var participantes = $("#participantes").val();
    var conteudoCertificado = $(".caixatexto").val();

    var data = new FormData();

    if (!filePlanoFundo) {
        toastr.warning('Insira um plano de fundo para o seu certificado.');
        return;
    }

    if (!participantes) {
        toastr.warning('Insira a lista de participantes.');
        return;
    }

    if (!conteudoCertificado) {
        toastr.warning('Escreva o conteúdo do certificado.');
        return;
    }

    $.blockUI({
        message: "<h3>Carregando...</h3>",
        css: {
            border: 'none',
            padding: '10px',
            backgroundColor: 'none',
            '-webkit-border-radius': '5px',
            '-moz-border-radius': '5px',
            opacity: 1,
            color: '#fff'
        }
    });

    data.append("planoFundo", filePlanoFundo);
    data.append("participantes", participantes);
    data.append("conteudoCertificado", conteudoCertificado);
    data.append("negrito", textoNegrito);
    data.append("italico", textoItalico);
    data.append("tamanhoFonte", tamanhoFonte);

    $.ajax({
        url: "/Certificado/Enviar",
        type: "POST",
        processData: false,
        contentType: false,
        data: data,
        success: function (response) {
            if (response.status == "success") {
                swal({
                    position: 'center',
                    type: 'success',
                    title: response.message,
                    showConfirmButton: true
                })
            } else {
                toastr.error(response.message);
            }

            $.unblockUI();
        },
        error: function (erro) {
            $.unblockUI();
            toastr.error("Ocorreu um erro");
        }
    });


}

$(document).ready(function () {

    function readURL(input) {
        if (input.files && input.files[0]) {
            var reader = new FileReader();

            reader.onload = function (e) {
                $img = $('<img/>').attr('src', e.target.result);
                $(".page-img-certificado").after($img);
            }

            reader.readAsDataURL(input.files[0]);

            filePlanoFundo = input.files[0];

            var text = document.createElement("textarea");
            var classe = "caixatexto";
            text.className = classe;
            text.style.padding = "4cm";
            text.style.fontSize = "50px";
            $(".page-img-certificado")[0].appendChild(text);
            $(".caixatexto").text(conteudoCertificado);
            $(".caixatexto").css("font-size", tamanhoFonte + "px");
            $(".caixatexto").css("text-align","center");
            if (textoNegrito) {
                $(".caixatexto").css("font-weight", "bold");
                $("#botao-negrito").css("box-shadow", "0 0 0 3px #c1c1c1");
            }
            if (textoItalico) {
                $(".caixatexto").css("font-style", "italic");
                $("#botao-italico").css("box-shadow", "0 0 0 3px #c1c1c1");
            }
            $("#area-ferramentas").css("display", "block");
        }
    }

    $('body').on("change", "input[type=file]", function () {
        conteudoCertificado = $(".caixatexto").val();
        $(".page-certificado img").remove();
        $(".page-certificado .caixatexto").remove();
        readURL(this);
    });

});
