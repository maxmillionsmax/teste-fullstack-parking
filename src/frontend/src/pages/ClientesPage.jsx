import React, { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiDelete, apiPut } from '../api'

export default function ClientesPage(){
  const qc = useQueryClient()
  const [filtro, setFiltro] = useState('')
  const [mensalista, setMensalista] = useState('all')
  const emptyForm = { nome:'', telefone:'', endereco:'', mensalista:false, valorMensalidade:'' }
  const [form, setForm] = useState(emptyForm)
  const [editId, setEditId] = useState(null)
  const [message, setMessage] = useState(null) // success or error message
  const [messageType, setMessageType] = useState('') // 'error' | 'success'

  useEffect(()=>{
    if (message) {
      const t = setTimeout(()=> setMessage(null), 5000)
      return () => clearTimeout(t)
    }
  }, [message])

  const q = useQuery({
    queryKey:['clientes', filtro, mensalista],
    queryFn:() => apiGet(`/api/clientes?pagina=1&tamanho=200&filtro=${encodeURIComponent(filtro)}&mensalista=${mensalista}`)
  })

  const create = useMutation({
    mutationFn: (data) => apiPost('/api/clientes', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey:['clientes'] })
      setForm(emptyForm)
      setMessage('Cliente criado com sucesso.')
      setMessageType('success')
    },
    onError: (err) => {
      setMessage(err?.message || 'Erro desconhecido')
      setMessageType('error')
    }
  })

  const update = useMutation({
    mutationFn: ({ id, data }) => apiPut(`/api/clientes/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey:['clientes'] })
      setForm(emptyForm)
      setEditId(null)
      setMessage('Cliente atualizado com sucesso.')
      setMessageType('success')
    },
    onError: (err) => {
      setMessage(err?.message || 'Erro desconhecido')
      setMessageType('error')
    }
  })

  const remover = useMutation({
    mutationFn: (id) => apiDelete(`/api/clientes/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey:['clientes'] })
      setMessage('Cliente removido.')
      setMessageType('success')
    },
    onError: (err) => {
      setMessage(err?.message || 'Erro desconhecido')
      setMessageType('error')
    }
  })

  function onSave(){
    // basic client-side validation
    if (!form.nome || form.nome.trim() === ''){
      setMessage('Nome é obrigatório.')
      setMessageType('error')
      return
    }
    const payload = {
      nome: form.nome,
      telefone: form.telefone || null,
      endereco: form.endereco || null,
      mensalista: !!form.mensalista,
      valorMensalidade: form.valorMensalidade ? Number(form.valorMensalidade) : null
    }
    if (editId){
      update.mutate({ id: editId, data: payload })
    } else {
      create.mutate(payload)
    }
  }

  function onEdit(client){
    setEditId(client.id)
    setForm({
      nome: client.nome || '',
      telefone: client.telefone || '',
      endereco: client.endereco || '',
      mensalista: !!client.mensalista,
      valorMensalidade: client.valorMensalidade ?? ''
    })
    setMessage(null)
  }

  function onCancelEdit(){
    setEditId(null)
    setForm(emptyForm)
    setMessage(null)
  }

  return (
    <div>
      <h2>Clientes</h2>

      <div className="section">
        <div className="grid grid-3">
          <input placeholder="Buscar por nome" value={filtro} onChange={e=>setFiltro(e.target.value)} />
          <select value={mensalista} onChange={e=>setMensalista(e.target.value)}>
            <option value="all">Todos</option>
            <option value="true">Mensalistas</option>
            <option value="false">Não mensalistas</option>
          </select>
          <div/>
        </div>
      </div>

      <h3>{editId ? 'Editar cliente' : 'Novo cliente'}</h3>
      <div className="section">
        <div className="grid grid-4">
          <input placeholder="Nome" value={form.nome} onChange={e=>setForm({...form, nome:e.target.value})}/>
          <input placeholder="Telefone" value={form.telefone} onChange={e=>setForm({...form, telefone:e.target.value})}/>
          <input placeholder="Endereço" value={form.endereco} onChange={e=>setForm({...form, endereco:e.target.value})}/>
          <label style={{display:'flex', alignItems:'center', gap:8}}>
            <input type="checkbox" checked={form.mensalista} onChange={e=>setForm({...form, mensalista:e.target.checked})}/> Mensalista
          </label>
          <input placeholder="Valor mensalidade" value={form.valorMensalidade} onChange={e=>setForm({...form, valorMensalidade:e.target.value})}/>
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
        {q.isLoading? <p>Carregando...</p> : (
          <table>
            <thead><tr><th>Nome</th><th>Telefone</th><th>Mensalista</th><th></th></tr></thead>
            <tbody>
              {q.data.itens.map(c=>(
                <tr key={c.id}>
                  <td>{c.nome}</td>
                  <td>{c.telefone}</td>
                  <td>{c.mensalista? 'Sim':'Não'}</td>
                  <td>
                    <button className="btn-ghost" onClick={()=>onEdit(c)}>Editar</button>
                    <button className="btn-ghost" onClick={()=>remover.mutate(c.id)}>Excluir</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
