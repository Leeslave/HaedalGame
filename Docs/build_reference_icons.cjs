// 시안의 SVG 선 아이콘을 그대로 프로젝트 PNG로 변환한다. 새 그림을 생성하지 않는다.
const fs = require('fs');
const path = require('path');
const sharp = require(process.argv[2] || 'sharp');
const icons = {
  Check:'M5 12.5l4.5 4.5L19 7.5',
  Reset:'M4 12a8 8 0 1 0 2.3-5.7M4 4v4h4',
  Bag:'M5 9h14l-1 11H6zM9 9V7a3 3 0 0 1 6 0v2',
  Hammer:'M4 20l9-9M11 5l3-2 7 7-2 3zM13 7l4 4',
  Menu:'M4 5.5A2 2 0 0 1 6 4h13v15H6a2 2 0 0 0-2 1.5zM4 5.5v15M8 8h7M8 11.5h7',
  Ingredient:'M2.5 12c3-4.5 8.5-5.5 13-2l5-3.5v11l-5-3.5c-4.5 3.5-10 2.5-13-2zM7.5 11h.01',
  Staff:'M12 11.5a3.75 3.75 0 1 0 0-7.5 3.75 3.75 0 0 0 0 7.5zM4.5 20.5a7.5 7.5 0 0 1 15 0',
  Furniture:'M3 9.5h18M6 9.5V20M18 9.5V20M5 9.5L7 5h10l2 4.5',
  Tools:'M9.5 19.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM15.5 10.5l6-3',
  Shop:'M4 10v10h16V10M3 10l2-6h14l2 6zM10 20v-5h4v5',
  Elf:'M5 19C5 10 11 5 20 4c-1 9-6 15-15 15zM5 19l8-8',
  Research:'M9 3h6M10 3v6l-5 9.5A1.8 1.8 0 0 0 6.6 21h10.8a1.8 1.8 0 0 0 1.6-2.5L14 9V3M7.5 15h9',
  Explore:'M3 15h18l-3 5H6zM12 15V4l6 8h-6',
  House:'M3.5 11L12 4l8.5 7M6 9.5V20h12V9.5M10 20v-5.5h4V20',
  Settings:'M12 2.8v3M12 18.2v3M2.8 12h3M18.2 12h3M5.5 5.5l2.1 2.1M16.4 16.4l2.1 2.1M5.5 18.5l2.1-2.1M16.4 7.6l2.1-2.1',
  Exit:'M10 4H5v16h5M14 8l4 4-4 4M18 12H9',
  New:'M6 13.5h12v5.5H6z M6 13.5a4 4 0 0 1 1.6-7.6A5 5 0 0 1 16.5 5a3.8 3.8 0 0 1 1.5 8.5',
  Play:'M7 4.5v15l12-7.5z'
};
const out=path.join(__dirname,'../Assets/Resources/UIDesign/Art');
fs.mkdirSync(out,{recursive:true});
Promise.all(Object.entries(icons).map(async([name,d])=>{
 const circle=name==='Settings'?'<circle cx="12" cy="12" r="3.2" fill="none" stroke="#fff" stroke-width="2.4"/>':'';
 const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="88" height="88" viewBox="0 0 24 24">${circle}<path d="${d}" fill="${name==='Play'?'#fff':'none'}" stroke="#fff" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
 await sharp(Buffer.from(svg)).png().toFile(path.join(out,'Icon'+name+'.png'));
})).then(()=>console.log(Object.keys(icons).length+' reference icons exported.'));
