const DB_KEY='rixu-planner-v3';
const OLD_KEY='focus-desk-tasks-v1';
const $=(s)=>document.querySelector(s);const $$=(s)=>[...document.querySelectorAll(s)];
const uid=()=>`${Date.now()}-${Math.random().toString(16).slice(2)}`;
const dateKey=(d=new Date())=>{const x=new Date(d.getTime()-d.getTimezoneOffset()*60000);return x.toISOString().slice(0,10)};
const today=dateKey();
const labels={year:'年度规划',quarter:'季度规划',month:'月度规划',week:'每周目标',today:'每日任务',habits:'习惯打卡'};
const icons={year:'◎',quarter:'◔',month:'☾',week:'◇',today:'✓',habits:'♧'};
const pageTitles={overview:'生活总览',year:'年度规划',quarter:'季度规划',month:'月度规划',week:'每周目标',today:'每日任务',habits:'习惯打卡',widget:'桌面配置'};

function load(){
  try{const saved=JSON.parse(localStorage.getItem(DB_KEY));if(saved)return saved}catch{}
  let old=[];try{old=JSON.parse(localStorage.getItem(OLD_KEY))||[]}catch{}
  return {items:{
    year:[item('建立更从容、有成长感的生活系统','让工作、身体和关系都获得稳定投入。','year','doing','2026-12-31')],
    quarter:[item('完成个人效率系统 2.0','把常用流程沉淀成真正顺手的工具。','quarter','doing','2026-09-30')],
    month:[item('稳定每周复盘节奏','连续完成四次周复盘。','month','doing',monthEnd())],
    week:[item('完成任务工具桌面版','让它值得每天打开。','week','doing',weekEnd()),item('安排两次有氧运动','每次至少 30 分钟。','week','todo',weekEnd()),item('整理本周关键资料','','week','todo',weekEnd())],
    today:old.length?old.map(x=>({...x,type:'today'})):[item('完成今天最重要的工作','','today','doing',today),item('阅读 20 分钟','','today','todo',today),item('整理桌面与收件箱','','today','done',today)]
  },habits:[{id:uid(),title:'运动 30 分钟',emoji:'🏃',history:[]},{id:uid(),title:'喝足 8 杯水',emoji:'💧',history:[today]},{id:uid(),title:'睡前阅读',emoji:'📖',history:[]}],config:[{id:'week',visible:true,collapsed:false},{id:'today',visible:true,collapsed:false},{id:'habits',visible:true,collapsed:false},{id:'month',visible:false,collapsed:false},{id:'quarter',visible:false,collapsed:true},{id:'year',visible:false,collapsed:true}]};
}
function item(title,note,type,status,date){return{id:uid(),title,note,type,status,date,createdAt:Date.now()}}
function monthEnd(){const d=new Date();return dateKey(new Date(d.getFullYear(),d.getMonth()+1,0))}
function weekEnd(){const d=new Date();d.setDate(d.getDate()+((7-d.getDay())%7));return dateKey(d)}
let data=load(),page='overview',search='',deferredInstall=null;
function save(){localStorage.setItem(DB_KEY,JSON.stringify(data));renderAll()}
function esc(v=''){return String(v).replace(/[&<>'"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]))}
function fmtDate(v){if(!v)return '未设日期';if(v===today)return '今天';const d=new Date(`${v}T00:00:00`);return `${d.getMonth()+1}月${d.getDate()}日`}
function progressOf(list){if(!list.length)return 0;return Math.round(list.filter(x=>x.status==='done').length/list.length*100)}
function allItems(){return Object.values(data.items).flat()}
function visible(list){if(!search)return list;const q=search.toLowerCase();return list.filter(x=>`${x.title} ${x.note}`.toLowerCase().includes(q))}
function periodLabel(type){return {year:'2026 YEAR',quarter:'Q3 QUARTER',month:'SEPTEMBER',week:'THIS WEEK',today:'TODAY'}[type]}

function renderWidget(){
  const todayItems=data.items.today.filter(x=>x.date===today||!x.date),p=progressOf(todayItems);$('#widgetProgress').textContent=`${p}%`;$('#widgetProgressRing').style.strokeDashoffset=113-(113*p/100);
  $('#widgetDate').textContent=new Intl.DateTimeFormat('zh-CN',{month:'long',day:'numeric',weekday:'short'}).format(new Date());
  const hour=new Date().getHours();$('#widgetGreeting').textContent=hour<11?'早上好，慢慢进入状态':hour<18?'下午好，保持自己的节奏':'晚上好，记得为今天收尾';
  const focus=[...data.items.today,...data.items.week].find(x=>x.status==='doing')||data.items.today.find(x=>x.status==='todo');$('#widgetFocusTitle').textContent=focus?.title||'今天已经圆满收尾啦';
  $('#widgetSections').innerHTML=data.config.filter(x=>x.visible).map((cfg,index)=>widgetSection(cfg,index)).join('');
  bindActions($('#widgetSections'));
  $$('.widget-section-head').forEach(btn=>btn.onclick=()=>{const cfg=data.config.find(x=>x.id===btn.dataset.section);cfg.collapsed=!cfg.collapsed;localStorage.setItem(DB_KEY,JSON.stringify(data));renderWidget()});
}
function widgetSection(cfg,index){
  const type=cfg.id,isHabit=type==='habits';let content='';let count=0;
  if(isHabit){count=data.habits.filter(h=>h.history.includes(today)).length;content=data.habits.map(h=>`<button class="mini-habit ${h.history.includes(today)?'checked':''}" data-habit="${h.id}"><b>${h.emoji}</b><strong>${esc(h.title)}</strong><span>${h.history.includes(today)?'今日完成':'轻触打卡'}</span></button>`).join('')}
  else{const list=data.items[type]||[];count=list.filter(x=>x.status!=='done').length;content=list.slice(0,4).map(x=>`<div class="mini-item ${x.status==='done'?'done':''}" data-id="${x.id}" data-type="${type}"><button data-toggle-item>✓</button><strong>${esc(x.title)}</strong><span>${fmtDate(x.date)}</span></div>`).join('')}
  if(!content)content='<div class="widget-empty">这里还很轻，去深度管理添加一项吧</div>';
  return `<section class="widget-section ${cfg.collapsed?'collapsed':''}" style="animation-delay:${index*.06}s"><button class="widget-section-head" data-section="${type}"><i>${icons[type]}</i><strong>${labels[type]}</strong><small>${isHabit?`${count}/${data.habits.length}`:`${count} 项进行中`}</small><em class="chevron">⌄</em></button><div class="widget-section-body">${content}</div></section>`;
}

function renderManager(){
  $('#managerDate').textContent=new Intl.DateTimeFormat('zh-CN',{year:'numeric',month:'long',day:'numeric',weekday:'long'}).format(new Date());$('#managerTitle').textContent=pageTitles[page];
  $('#backButton').classList.toggle('hidden',page==='overview');
  $('#weekNavCount').textContent=data.items.week.filter(x=>x.status!=='done').length;$('#todayNavCount').textContent=data.items.today.filter(x=>x.status!=='done').length;
  $$('#managerNav button').forEach(b=>b.classList.toggle('active',b.dataset.page===page));
  if(page==='overview')renderOverview();else if(page==='habits')renderHabitPage();else if(page==='widget')renderConfig();else renderPlanPage(page);
}
function renderOverview(){
  const todayList=data.items.today,p=progressOf(data.items.week);const planTypes=['year','quarter','month','week'];
  $('#managerContent').innerHTML=`<div class="welcome"><article class="welcome-card"><span>WELCOME BACK · 今天也值得期待</span><h2>把远方放进计划，<br>把计划轻轻放进今天。</h2><p>${todayList.filter(x=>x.status!=='done').length} 项今日行动正在等你，不着急，一件一件来。</p></article><article class="week-score"><div><h3>本周完成度</h3><p>每一个小勾，都是可见的前进</p></div><div class="big-ring" style="--progress:${p}%"><b>${p}%</b></div></article></div><div class="section-heading"><div><span>PLANNING PATH</span><h2>从愿景到本周</h2></div><button class="text-button" data-go="year">展开规划 →</button></div><div class="planning-ribbon">${planTypes.map((t,i)=>planPreview(t,i)).join('')}</div><div class="section-heading"><div><span>HERE & NOW</span><h2>今天，落地一点点</h2></div><button class="text-button" data-add="today">＋ 添加任务</button></div><div class="dashboard-grid"><article class="surface"><div class="surface-head"><h3>今日任务</h3><span>${progressOf(todayList)}% 已完成</span></div><div class="item-list">${visible(todayList).map(taskRow).join('')||empty('今天还没有任务')}</div></article><article class="surface"><div class="surface-head"><h3>今日打卡</h3><span>连续行动更重要</span></div><div class="habit-grid">${data.habits.map(habitCard).join('')}</div></article></div>`;bindActions($('#managerContent'));
}
function planPreview(type,index){const list=data.items[type],top=list.find(x=>x.status!=='done')||list[0],p=progressOf(list);return `<article class="plan-card" style="animation-delay:${index*.05}s"><i class="plan-icon">${icons[type]}</i><small>${periodLabel(type)}</small><h3>${esc(top?.title||`添加${labels[type]}`)}</h3><div class="tiny-progress"><i style="width:${p}%"></i></div><button data-go="${type}">•••</button></article>`}
function taskRow(x){return `<div class="task-row ${x.status==='done'?'done':''}" data-id="${x.id}" data-type="${x.type}"><button class="check" data-toggle-item>✓</button><div><h4>${esc(x.title)}</h4>${x.note?`<p>${esc(x.note)}</p>`:''}</div><time>${fmtDate(x.date)}</time></div>`}
function habitCard(h){const done=h.history.includes(today);return `<button class="habit-card ${done?'checked':''}" data-habit="${h.id}"><span class="emoji">${h.emoji}</span><strong>${esc(h.title)}</strong><small>${done?'今天做到了':'轻触完成今日打卡'}</small><i class="tick">✓</i></button>`}
function renderPlanPage(type){const list=visible(data.items[type]);$('#managerContent').innerHTML=`<div class="section-heading"><div><span>${periodLabel(type)}</span><h2>${labels[type]}</h2><p>${type==='week'?'把本周最重要的事控制在看得见的范围内':'给长期方向一个清晰、可调整的位置'}</p></div><button class="round-add" data-add="${type}">＋</button></div><div class="page-grid">${list.map((x,i)=>goalCard(x,i)).join('')||empty(`还没有${labels[type]}`)}</div>`;bindActions($('#managerContent'))}
function goalCard(x,i){const p=x.status==='done'?100:x.status==='doing'?55:12;return `<article class="goal-card" style="animation-delay:${i*.05}s" data-id="${x.id}" data-type="${x.type}"><div class="goal-top"><span class="goal-type">${x.status==='done'?'已完成':x.status==='doing'?'正在推进':'待开始'}</span><button class="more" data-edit>•••</button></div><h3>${esc(x.title)}</h3><p>${esc(x.note||'给它补充一句为什么重要吧。')}</p><footer><span>${p}%</span><div class="tiny-progress"><i style="width:${p}%"></i></div><button class="check ${x.status==='done'?'checked':''}" data-toggle-item>✓</button></footer></article>`}
function renderHabitPage(){const done=data.habits.filter(h=>h.history.includes(today)).length;$('#managerContent').innerHTML=`<div class="welcome"><article class="welcome-card"><span>DAILY RHYTHM</span><h2>照顾身体，也是计划的一部分。</h2><p>今天完成 ${done}/${data.habits.length} 项，不追求完美，只保持连接。</p></article><article class="week-score"><div><h3>今日打卡</h3><p>轻触卡片即可完成</p></div><div class="big-ring" style="--progress:${data.habits.length?done/data.habits.length*100:0}%"><b>${done}/${data.habits.length}</b></div></article></div><div class="section-heading"><div><span>MY HABITS</span><h2>每天想照顾的小事</h2></div><button class="round-add" data-add="habits">＋</button></div><div class="page-grid">${data.habits.map(h=>`<article class="goal-card"><div class="goal-top"><span class="goal-type">${h.emoji} 习惯</span><button class="more" data-edit-habit="${h.id}">•••</button></div><h3>${esc(h.title)}</h3><p>累计打卡 ${h.history.length} 天</p><footer><button class="habit-card ${h.history.includes(today)?'checked':''}" data-habit="${h.id}"><strong>${h.history.includes(today)?'今日已完成 ✓':'完成今日打卡'}</strong></button></footer></article>`).join('')}</div>`;bindActions($('#managerContent'))}
function renderConfig(){$('#managerContent').innerHTML=`<div class="welcome"><article class="welcome-card"><span>DESKTOP WIDGET</span><h2>只把此刻需要的，留在桌面。</h2><p>默认前三项会进入首屏；启用更多模块后，可在桌面小组件中向下滚动查看。</p></article><article class="week-score"><div><h3>已启用模块</h3><p>推荐保持 3 项，轻而专注</p></div><div class="big-ring" style="--progress:${data.config.filter(x=>x.visible).length/6*100}%"><b>${data.config.filter(x=>x.visible).length}/6</b></div></article></div><div class="section-heading"><div><span>WIDGET SETTINGS</span><h2>桌面展示顺序</h2></div></div><div class="config-list">${data.config.map((c,i)=>`<article class="config-row"><i>${icons[c.id]}</i><div><strong>${labels[c.id]}</strong><small>${i<3?'首屏优先展示':'启用后向下滚动查看'}</small></div><div class="order-actions"><button data-move="-1" data-module="${c.id}">↑</button><button data-move="1" data-module="${c.id}">↓</button></div><button class="toggle ${c.visible?'on':''}" data-module-toggle="${c.id}" aria-label="切换显示"></button></article>`).join('')}</div>`;bindActions($('#managerContent'))}
function empty(t){return `<div class="empty-state"><div><i>☁</i>${t}<br>这里可以慢慢长出新的计划</div></div>`}

function bindActions(root){
  root.querySelectorAll('[data-toggle-item]').forEach(b=>b.onclick=()=>{const row=b.closest('[data-id]'),x=data.items[row.dataset.type].find(i=>i.id===row.dataset.id);x.status=x.status==='done'?'todo':'done';save();burst(b)});
  root.querySelectorAll('[data-habit]').forEach(b=>b.onclick=()=>{const h=data.habits.find(x=>x.id===b.dataset.habit),i=h.history.indexOf(today);i>=0?h.history.splice(i,1):h.history.push(today);save();burst(b)});
  root.querySelectorAll('[data-add]').forEach(b=>b.onclick=()=>openEditor(b.dataset.add));root.querySelectorAll('[data-edit]').forEach(b=>b.onclick=()=>{const row=b.closest('[data-id]');openEditor(row.dataset.type,row.dataset.id)});
  root.querySelectorAll('[data-go]').forEach(b=>b.onclick=()=>go(b.dataset.go));root.querySelectorAll('[data-module-toggle]').forEach(b=>b.onclick=()=>{data.config.find(x=>x.id===b.dataset.moduleToggle).visible=!data.config.find(x=>x.id===b.dataset.moduleToggle).visible;save()});
  root.querySelectorAll('[data-move]').forEach(b=>b.onclick=()=>{const i=data.config.findIndex(x=>x.id===b.dataset.module),n=i+Number(b.dataset.move);if(n<0||n>=data.config.length)return;[data.config[i],data.config[n]]=[data.config[n],data.config[i]];save()});
}
function go(next){page=next;renderManager()}
function openEditor(type,id=''){const isHabit=type==='habits',x=isHabit?data.habits.find(h=>h.id===id):data.items[type]?.find(i=>i.id===id);$('#editorForm').reset();$('#editorType').value=type;$('#editorId').value=id;$('#editorTitle').value=x?.title||'';$('#editorNote').value=x?.note||'';$('#editorDate').value=x?.date||today;$('#editorStatus').value=x?.status||'todo';$('#editorHeading').textContent=id?'调整这项内容':`添加${labels[type]||'内容'}`;$('#editorEyebrow').textContent=isHabit?'日常节奏':periodLabel(type)||'NEW ITEM';$('#deleteItem').classList.toggle('hidden',!id);$('#editorModal').classList.remove('hidden');setTimeout(()=>$('#editorTitle').focus(),40)}
function closeEditor(){$('#editorModal').classList.add('hidden')}
function renderAll(){renderWidget();renderManager()}
function burst(el){el.animate([{transform:'scale(.75)'},{transform:'scale(1.18)'},{transform:'scale(1)'}],{duration:330,easing:'ease-out'})}
let toastTimer;function toast(t){$('#toast').textContent=t;$('#toast').classList.add('show');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('#toast').classList.remove('show'),2200)}

function setup(){
  const params=new URLSearchParams(location.search),widget=params.has('widget')&&!params.has('manage'),pocket=params.has('pocket');document.title=widget?(pocket?'Rixu Pocket':'Rixu Desktop Widget'):'Rixu Manager';document.body.classList.toggle('widget-mode',widget);
  $('#globalSearch').oninput=e=>{search=e.target.value;renderManager()};$('#globalAdd').onclick=()=>openEditor(page==='overview'?'today':page==='widget'?'week':page);
  $('#backButton').onclick=()=>go('overview');
  $('#pocketEntry').onclick=()=>{const url=new URL(location.href);url.search='?widget=1&pocket=1';window.open(url.href,'rixu-pocket','popup,width=420,height=600,resizable=yes')};
  $('#desktopEntry').onclick=()=>{location.href='rixu://desktop'};
  $('#exitManager').onclick=()=>{location.href='rixu://exit-manager'};
  $$('#managerNav button').forEach(b=>b.onclick=()=>go(b.dataset.page));$$('[data-close-modal]').forEach(b=>b.onclick=closeEditor);$('#editorModal').onclick=e=>{if(e.target===$('#editorModal'))closeEditor()};
  $('#editorForm').onsubmit=e=>{e.preventDefault();const type=$('#editorType').value,id=$('#editorId').value,title=$('#editorTitle').value.trim();if(type==='habits'){if(id)data.habits.find(x=>x.id===id).title=title;else data.habits.push({id:uid(),title,emoji:'🌱',history:[]})}else{const values={title,note:$('#editorNote').value.trim(),date:$('#editorDate').value,status:$('#editorStatus').value,type};if(id)Object.assign(data.items[type].find(x=>x.id===id),values);else data.items[type].push({...values,id:uid(),createdAt:Date.now()})}closeEditor();save();toast(id?'已更新':'已添加')};
  $('#deleteItem').onclick=()=>{const type=$('#editorType').value,id=$('#editorId').value;if(type==='habits')data.habits=data.habits.filter(x=>x.id!==id);else data.items[type]=data.items[type].filter(x=>x.id!==id);closeEditor();save();toast('已删除')};
  $('#widgetClose').onclick=()=>{if(window.opener)window.close();else location.href='rixu://close-pocket'};$('#widgetRefresh').onclick=()=>{renderWidget();toast('今天的状态已刷新')};
  window.addEventListener('beforeinstallprompt',e=>{e.preventDefault();deferredInstall=e});
  if('serviceWorker'in navigator&&location.protocol.startsWith('http'))navigator.serviceWorker.register('./sw.js').catch(()=>{});
  renderAll();setTimeout(()=>$('#splash').classList.add('hide'),650);setTimeout(()=>$('#splash').remove(),1250);
}
setup();
