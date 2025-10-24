import React from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom'
import ClientesPage from './pages/ClientesPage'
import VeiculosPage from './pages/VeiculosPage'
import FaturamentoPage from './pages/FaturamentoPage'
import CsvUploadPage from './pages/CsvUploadPage'
import './styles.css'

const qc = new QueryClient()

function Layout(){
  return (
    <div className="app">
      <h1>Parking</h1>
      <nav>
        <Link to="/">Clientes</Link>
        <Link to="/veiculos">Veículos</Link>
        <Link to="/faturamento">Faturamento</Link>
        <Link to="/csv">Importar CSV</Link>
      </nav>
      <hr/>
      <Routes>
        <Route path="/" element={<ClientesPage/>}/>
        <Route path="/veiculos" element={<VeiculosPage/>}/>
        <Route path="/faturamento" element={<FaturamentoPage/>}/>
        <Route path="/csv" element={<CsvUploadPage/>}/>
      </Routes>
    </div>
  )
}

createRoot(document.getElementById('root')).render(
  <QueryClientProvider client={qc}>
    <BrowserRouter>
      <Layout/>
    </BrowserRouter>
  </QueryClientProvider>
)
