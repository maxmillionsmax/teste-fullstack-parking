import React, { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost } from '../api'

export default function FaturamentoPage(){
  const [comp, setComp] = useState('2025-08')
  const faturas = useQuery({ queryKey:['faturas', comp], queryFn:() => apiGet(`/api/faturas?competencia=${comp}`) })

  return (
    <div>
      <h2>Faturamento</h2>

      <div className="section">
        <div style={{display:'flex', gap:10, alignItems:'center'}}>
          <input value={comp} onChange={e=>setComp(e.target.value)} placeholder="yyyy-MM" />
          <button onClick={async ()=>{
            await apiPost('/api/faturas/gerar', { competencia: comp })
            faturas.refetch()
          }}>Gerar faturas</button>
        </div>
      </div>

      <h3 style={{marginTop:16}}>Faturas</h3>
      <div className="section">
        {faturas.isLoading? <p>Carregando...</p> : (
          <table>
            <thead><tr><th>Cliente</th><th>Competência</th><th>Valor</th><th>Qtd Veículos</th><th>Placas</th></tr></thead>
            <tbody>
              {faturas.data?.map(f=>(<FaturaRow key={f.id} f={f} />))}
            </tbody>
          </table>
        )}
        <p className="note">BUG proposital: API associa veículos pelo dono atual, e não pelo dono na data de corte.</p>
      </div>
    </div>
  )
}

function FaturaRow({f}){
  const [show, setShow] = useState(false)
  const [placas, setPlacas] = useState([])
  return (
    <tr>
      <td>{f.clienteId}</td>
      <td>{f.competencia}</td>
      <td>{Number(f.valor).toFixed(2)}</td>
      <td>{f.qtdVeiculos}</td>
      <td>
        <button className="btn-ghost" onClick={async ()=>{
          if(!show){
            const r = await apiGet(`/api/faturas/${f.id}/placas`)
            setPlacas(r)
          }
          setShow(s=>!s)
        }}>{show?'ocultar':'detalhar'}</button>
        {show && <div style={{marginTop:6}}>{placas.join(', ')}</div>}
      </td>
    </tr>
  )
}
