import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

if (environment.production) {
  enableProdMode();
}

platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.log(err));


/**
 * Storage prefix
 */
const storagePrefix = 'client_';

let originStorageSetItem = Storage.prototype.setItem;
Storage.prototype.setItem = function(key, value) {
  return originStorageSetItem.call(this, storagePrefix + key, value);
}

let originStorageGetItem = Storage.prototype.getItem;
Storage.prototype.getItem = function(key) {
  return originStorageGetItem.call(this,storagePrefix + key);
}

let originStorageRemoveItem = Storage.prototype.removeItem;
Storage.prototype.removeItem = function(key) {
  return originStorageRemoveItem.call(this, storagePrefix + key);
}

/**
 *  metrics and jivosite
 */
if (environment.isProduction) {
    document.write(`
     <!-- Yandex.Metrika counter -->
    <script type="text/javascript" >
        (function(m,e,t,r,i,k,a){m[i]=m[i]||function(){(m[i].a=m[i].a||[]).push(arguments)};
            m[i].l=1*new Date();k=e.createElement(t),a=e.getElementsByTagName(t)[0],k.async=1,k.src=r,a.parentNode.insertBefore(k,a)})
        (window, document, "script", "https://mc.yandex.ru/metrika/tag.js", "ym");
  
        ym(53455318, "init", {
            clickmap:true,
            trackLinks:true,
            accurateTrackBounce:true,
            webvisor:true
        });
    </script>
    <noscript><div><img src="https://mc.yandex.ru/watch/53455318" style="position:absolute; left:-9999px;" alt="" /></div></noscript>
    <!-- /Yandex.Metrika counter -->
  
    <!-- Global site tag (gtag.js) - Google Analytics -->
    <script async src="https://www.googletagmanager.com/gtag/js?id=UA-135679645-3"></script>
    <script>
        window.dataLayer = window.dataLayer || [];
        function gtag(){dataLayer.push(arguments);}
        gtag('js', new Date());
  
        gtag('config', 'UA-135679645-3');
    </script>
    <script src="//code3.jivosite.com/widget.js" data-jv-id="GzNxkXh6Tf" async></script>
  `);
}
