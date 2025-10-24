import React, { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, apiDelete } from '../api'

export default function VeiculosPage(){
  const qc = useQueryClient()
  const [clienteId, setClienteId] = useState('')
  const [editId, setEditId] = useState(null)
  const [message, setMessage] = useState(null)
  const [messageType, setMessageType] = useState('') // 'error' | 'success'

  const clientes = useQuery({
    queryKey:['clientes-mini'],
    queryFn:() => apiGet('/api/clientes?pagina=1&tamanho=100')
  })

  const veiculos = useQuery({
    queryKey:['veiculos', clienteId],
    queryFn:() => apiGet(`/api/veiculos${clienteId?`?clienteId=${clienteId}`:''}`)
  })

  const emptyForm = { placa:'', modelo:'', ano:'', clienteId:'' }
  const [form, setForm] = useState(emptyForm)

  useEffect(()=>{
    if (message) {
      const t = setTimeout(()=> setMessage(null), 5000)
      return () => clearTimeout(t)
    }
  }, [message])

  useEffect(()=>{
    if(clientes.data?.itens?.length && !clienteId){
      setClienteId(clientes.data.itens[0].id)
      setForm(f => ({...f, clienteId: clientes.data.itens[0].id}))
    }
  }, [clientes.data])

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/veiculos', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey:['veiculos'] })
      setForm(emptyForm)
      setMessage('Veículo criado com sucesso.')
      setMessageType('success')
    },
    onError: (err) => {
      setMessage(err?.message || 'Erro desconhecido')
      setMessageType('error')
    }
  })

  const update = useMutation({
    mutationFn: ({id, data}) => apiPut(`/api/veiculos/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey:['veiculos'] })
      setForm(emptyForm)
      setEditId(null)
      setMessage('Veículo atualizado com sucesso.')
      setMessageType('success')
    },
    onError: (err) => {
      setMessage(err?.message || 'Erro desconhecido')
      setMessageType('error')
    }
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/veiculos/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey:['veiculos'] })
      setMessage('Veículo removido.')
      setMessageType('success')
      // if we were editing the removed vehicle, reset form
      if (editId === id) {
        setEditId(null)
        setForm(emptyForm)
      }
    },
    onError: (err) => {
      setMessage(err?.message || 'Erro desconhecido')
      setMessageType('error')
    }
  })

  function onSave(){
    // basic client-side validation
    if (!form.placa || form.placa.trim() === ''){
      setMessage('Placa é obrigatória.')
      setMessageType('error')
      return
    }
    if (!form.clienteId){
      setMessage('Selecione um cliente.')
      setMessageType('error')
      return
    }

    const payload = {
      placa: form.placa,
      modelo: form.modelo || null,
      ano: form.ano ? Number(form.ano) : null,
      clienteId: form.clienteId
    }

    if (editId){
      update.mutate({ id: editId, data: payload })
    } else {
      create.mutate(payload)
    }
  }

  function onEdit(v){
    setEditId(v.id)
    setForm({
      placa: v.placa || '',
      modelo: v.modelo || '',
      ano: v.ano ?? '',
      clienteId: v.clienteId || clienteId
    })
    setMessage(null)
  }

  function onCancelEdit(){
    setEditId(null)
    setForm(emptyForm)
    setMessage(null)
  }

  // helper to map clienteId to nome
  const clienteMap = (clientes.data?.itens || []).reduce((acc, c) => (acc[c.id] = c.nome, acc), {})

  return (
    <div>
      <h2>Veículos</h2>

      <div className="section">
        <div style={{display:'flex', gap:10, alignItems:'center'}}>
          <label>Filtrar por cliente: </label>
          <select value={clienteId} onChange={e=>{ setClienteId(e.target.value); setForm(f=>({...f, clienteId:e.target.value}))}}>
            {clientes.data?.itens?.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
      </div>

      <h3>{editId ? 'Editar veículo' : 'Novo veículo'}</h3>
      <div className="section">
        <div className="grid grid-4">
          <input placeholder="Placa" value={form.placa}
                 onChange={e=>setForm({...form, placa:e.target.value})}
                 disabled={!!editId} />
          <input placeholder="Modelo" value={form.modelo} onChange={e=>setForm({...form, modelo:e.target.value})}/>
          <input placeholder="Ano" value={form.ano} onChange={e=>setForm({...form, ano:e.target.value})}/>
          <select value={form.clienteId || ''} onChange={e=>setForm({...form, clienteId:e.target.value})}>
            <option value="">-- selecione --</option>
            {clientes.data?.itens?.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>

          <div/>
          <div/>
          <div style={{display:'flex', gap:8}}>
            <button onClick={onSave}>{editId ? 'Atualizar' : 'Salvar'}</button>
            {editId && <button onClick={onCancelEdit} className="btn-ghost">Cancelar</button>}
          </div>
        </div>

        {message && (
          <div style={{marginTop:12, color: messageType === 'error' ? 'crimson' : 'green'}}>
            {message}
          </div>
        )}
      </div>

      <h3 style={{marginTop:16}}>Lista</h3>
      <div className="section">
        {veiculos.isLoading? <p>Carregando...</p> : (
          <table>
            <thead><tr><th>Placa</th><th>Modelo</th><th>Ano</th><th>Cliente</th><th>Ações</th></tr></thead>
            <tbody>
              {veiculos.data?.map(v=>(
                <tr key={v.id}>
                  <td>{v.placa}</td>
                  <td>{v.modelo || '-'}</td>
                  <td>{v.ano ?? '-'}</td>
                  <td>{clienteMap[v.clienteId] || v.clienteId}</td>
                  <td style={{display:'flex', gap:8}}>
                    <button className="btn-ghost" onClick={()=>onEdit(v)}>Editar</button>
                    <button className="btn-ghost" onClick={()=>remover.mutate(v.id)}>Excluir</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <p className="note">Dica: ao editar, a placa fica read-only. Você pode alterar Modelo, Ano e reatribuir o cliente.</p>
      </div>
    </div>
  )
}
