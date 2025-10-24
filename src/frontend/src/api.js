
const API = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export async function apiGet(path){
  const r = await fetch(API + path)
  if(!r.ok) throw new Error(await r.text())
  return r.json()
}
export async function apiPost(path, body){
  const r = await fetch(API + path, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body)})
  if(!r.ok) throw new Error(await r.text())
  return r.json()
}
export async function apiPut(path, body){
  const r = await fetch(API + path, { method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body)})
  if(!r.ok) throw new Error(await r.text())
  return r.json()
}
export async function apiDelete(path){
  const r = await fetch(API + path, { method:'DELETE' })
  if(!r.ok) throw new Error(await r.text())
  return r.text()
}
