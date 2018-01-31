<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />

  <style media="all" type="text/css">
    @font-face{font-family:"Gotham Pro";font-weight:300;src:url("fonts/GothamPro-Light.woff") format("woff"),url("fonts/GothamPro-Light.ttf") format("truetype"),url("fonts/GothamPro-Light.eot?") format("eot");}
    @font-face{font-family:"Gotham Pro";font-weight:400;src:url("fonts/GothamPro.woff") format("woff"),url("fonts/GothamPro.ttf") format("truetype"),url("fonts/GothamPro.eot?") format("eot");}
    @font-face{font-family:"Gotham Pro";font-weight:500;src:url("fonts/GothamPro-Medium.woff") format("woff"),url("fonts/GothamPro-Medium.ttf") format("truetype"),url("fonts/GothamPro-Medium.eot?") format("eot");}
  </style>

  <style media="all" type="text/css">
    html{font-family:"Gotham Pro","Proxima Nova",Lato,Verdana,Geneva,sans-serif;font-size:16px;line-height:1.2;height:100%;}
    body{margin:0;padding:0;min-height:100%;display:-webkit-box;display:-ms-flexbox;display:flex;-webkit-box-orient:vertical;-webkit-box-direction:normal;-webkit-flex-direction:column;-ms-flex-direction:column;flex-direction:column;}
    .container{width:100%;max-width:1120px;padding-right:10px;padding-left:10px;margin-right:auto;margin-left:auto;}
    .header{background-color:#1c1c1c;color:#fff;min-height:78px;font-size:18px;letter-spacing:.05em;}
    .header *{vertical-align:middle;}
    .footer{background-color:#1c1c1c;color:#fff;height:30px;line-height:30px;font-size:14px;font-weight:300;margin-top:auto;}
    .logo{margin:23px 16px 23px 0;height:32px;width:32px;}
    .page__heading{font-size:3rem;font-weight:300;letter-spacing:.1em;text-transform:uppercase;margin:0;padding:45px 0 25px;}
    .section{margin-bottom:60px;}
    .section__title{font-size:18px;font-weight:500;line-height:32px;margin-top:18px;border-bottom:3px solid #e9cb6b;margin-bottom:26px;}
    .section__content{padding-bottom:25px;border-bottom:3px solid #f5f5f5;}
    .clear-fix:after{content:"";display:table;clear:both;}
    .amount{white-space:nowrap;}
    .amount > *{display:inline-block;}
    .amount input{width:180px;}
    .amount__currency{font-size:18px;font-weight:500;line-height:36px;margin-left:12px;}
    .amount__currency *{vertical-align:middle;}
    .amount__currency-sign{margin-right:2px;}
    .expire{font-size:18px;}
    .cvv{display:-webkit-box;display:-ms-flexbox;display:flex;-webkit-box-align:center;-ms-flex-align:center;align-items:center;-webkit-box-pack:justify;-ms-flex-pack:justify;justify-content:space-between;-webkit-flex-grow:1;flex-grow:1;}
    .cvv input{width:85px;}
    .security{display:-webkit-box;display:-ms-flexbox;display:flex;-webkit-box-align:center;-ms-flex-align:center;align-items:center;padding-top:10px;}
    .security > *:not(:first-child){margin-left:30px;}
    .description{color:#333;font-size:18px;margin-top:0;}
    .row{display:-webkit-box;display:-ms-flexbox;display:flex;-ms-flex-wrap:wrap;flex-wrap:wrap;}
    .form-col{width:300px;margin-right:50px;}
    .form-col-last{margin-left:auto;padding-top:16px;}
    .form-group{margin-bottom:20px;}
    .form-group__wide input{width:275px;}
    label{color:#7a7a7a;font-size:18px;line-height:24px;display:block;margin-bottom:5px;}
    label .icon{vertical-align:text-top;}
    input[type="text"],select{color:#1c1c1c;font-size:18px;font-family:inherit;line-height:22px;padding:6px 10px;border:2px solid #f5f5f5;}
    input[readonly]{background-color:#f5f5f5;}
    input[type="submit"]{color:#1c1c1c;font-size:18px;font-family:inherit;font-weight:700;line-height:22px;text-transform:uppercase;letter-spacing:.1em;background-color:#e9cb6b;border:2px solid #e9cb6b;padding:16px 26px;transition:all .16s ease-out;cursor:pointer;}
    input[type="submit"]:hover{background-color:#fadc7d;border-color:#fadc7d;}
  </style>
</head>

<body>
  <header class="header">
    <div class="container">
      <img src="images/logo.svg" alt="GoldMint.io" width="32" height="32" class="logo">
      <span>GoldMint.io</span>
    </div>
  </header>

  <main class="container" role="main">
    <div class="page">
      <h1 class="page__heading">Deposit</h1>

      <section class="page__section section">
        <h2 class="section__title">Card details</h2>

        <div class="section__content clear-fix">
          {start_form}

            <div class="row">
              <div class="form-col">
                <div class="form-group">
                  <label for="amount">Amount:</label>

                  <div class="amount">
                    <input type="text" class="form-control" id="amount" value="{amount}" readonly />

                    <div class="amount__currency">
                      <img src="images/cur-usd.svg" alt="USD" width="19" height="32" class="amount__currency-sign" id="amount_currency_sign" />
                      <span id="amount_currency">{currency}</span>
                    </div>
                  </div>
                </div>

                <div class="form-group">
                  <label>Description:</label>
                  <p class="description">{description}</p>
                </div>
              </div>

              <div class="form-col">
                <div class="form-group form-group__wide">
                  <label for="name_on_cc">Name on card:</label>
                  {input_name}
                </div>

                <div class="form-group form-group__wide">
                  <label for="cc">Card number:</label>
                  {input_cc}
                </div>

                <div class="form-group">
                  <label for="cc_expire_month">Cc expire:</label>

                  <div class="expire">
                    {CC_EXPIRE}
                  </div>
                </div>
              </div>

              <div class="form-col-last hide-on-mobile">
                {submit_form}
              </div>
            </div>

            <div class="row">
              <div class="form-col"></div>

              <div class="cvv">
                <div class="form-group">
                  <label for="cvv">CVV: <a href="javascript:void(0)"><img src="images/info.svg" alt="What is this?" title="Turn your card over and look at the signature box. You should see either the entire 16-digit credit card number or just the last four digits followed by a special 3-digit code. This 3-digit code is your CVV number." width="18" height="18" class="icon"></a></label>
                  {input_cvv}
                </div>

                <div class="security">
                  <img src="images/security_mastercard.png" srcset="images/security_mastercard@2x.png 2x"
                       width="104" height="37" alt="MasterCard SecureCode" />

                  <img src="images/security_visa.png" srcset="images/security_visa@2x.png 2x"
                       width="89" height="38" alt="Verified by VISA" />

                  <img src="images/security_pci.png" srcset="images/security_pci@2x.png 2x"
                       width="152" height="60" alt="PCI Security Standards Council" />
                </div>
              </div>
            </div>
          {end_form}
        </div>
      </section>

    </div>
  </main>

  <footer class="footer">
    <div class="container">GoldMint, PTE, LTD © 2017</div>
  </footer>

  {default_js}
</body>
</html>
