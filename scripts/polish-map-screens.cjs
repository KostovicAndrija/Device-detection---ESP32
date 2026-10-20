const fs=require('node:fs');const path=require('node:path');const {chromium}=require(process.argv[2]);
const out=path.resolve(__dirname,'../screenshots/2026-10-20');const state=JSON.parse(fs.readFileSync(path.join(out,'demo-state.json'),'utf8'));
(async()=>{
  const browser=await chromium.launch({channel:'chrome',headless:true,args:['--disk-cache-size=1048576','--disable-gpu-shader-disk-cache']});
  try{
    const context=await browser.newContext({viewport:{width:1600,height:1100},deviceScaleFactor:1.5,locale:'sr-Latn-RS',timezoneId:'Europe/Belgrade',reducedMotion:'reduce'});
    const auth=await (await fetch('http://localhost:7108/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({username:'admin',password:'admin123'})})).json();
    await context.addInitScript(auth=>localStorage.setItem('device-detection-auth',JSON.stringify(auth)),auth);
    const page=await context.newPage();await page.goto(state.url,{waitUntil:'networkidle'});
    await page.waitForFunction(()=>document.querySelectorAll('.room-shell .device-marker.live').length===3&&document.querySelectorAll('.room-shell .risk-warning').length===1&&document.querySelectorAll('.room-shell .risk-critical').length===1,null,{timeout:60000});
    await page.evaluate(()=>document.fonts.ready);
    await page.screenshot({path:path.join(out,'04-mapa-uzivo.png'),fullPage:true,animations:'disabled'});
    await page.locator('.room-shell').screenshot({path:path.join(out,'05-mapa-detalj.png'),animations:'disabled'});
    await page.getByRole('button',{name:'Uvećaj mapu',exact:true}).click();
    await page.screenshot({path:path.join(out,'06-mapa-uvecana.png'),animations:'disabled'});
    console.log('Verified: 3 live devices, exactly one critical desk, exactly one warning desk.');
  }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
