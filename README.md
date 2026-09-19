# 🇧🇷 SolidSign API - Exemplo de Integração: Assinatura CAdES (CMS) com HSM/Nuvem (C#)

## Requisitos

- ASP.NET Minimal API
- Um token JWT válido (`POST /solidsign/auth/token`)
- Front-end de referência (opcional): [`exemplo-react-cms-cloud`](https://github.com/SolidTechSolutions/exemplo-react-cms-cloud)

## Como rodar

```bash
dotnet run
```

O serviço sobe em `http://localhost:5093`.

## Como funciona

Este back-end expõe o endpoint abaixo, que recebe um formulário (`multipart/form-data`, CORS liberado) e repassa os dados pra SolidSign API real, devolvendo o resultado:

- `POST /api/cms/sign/form`

## Variáveis do formulário

| Campo | Significado | Default |
|---|---|---|
| `document[i]` | Documento(s) a assinar | — |
| `authorization` | Token JWT (Bearer) | — |
| `baseUrl` | URL base da SolidSign API | `https://www.solidsign.com.br` |
| `cloudCredentials` | Credenciais do HSM/PSC de nuvem (JSON: hsmUrl, hsmToken, uuidCert) | — |
| `profile` | Perfil de assinatura PBAD/ETSI | `ADRB` |
| `hashAlgorithm` | Algoritmo de hash | `SHA256` |
| `signaturePackaging` | Empacotamento CMS | `ATTACHED` |

---

# 🇬🇧 SolidSign API - Integration Example: CAdES (CMS) Signing with HSM/Cloud (C#)

## Requirements

- ASP.NET Minimal API
- A valid JWT token (`POST /solidsign/auth/token`)
- Reference front-end (optional): [`exemplo-react-cms-cloud`](https://github.com/SolidTechSolutions/exemplo-react-cms-cloud)

## Running

```bash
dotnet run
```

The service starts on `http://localhost:5093`.

## How it works

This backend exposes the endpoint below, which accepts a form (`multipart/form-data`, CORS-enabled) and forwards the data to the real SolidSign API, returning the result:

- `POST /api/cms/sign/form`

## Form fields

| Field | Meaning | Default |
|---|---|---|
| `document[i]` | Document(s) to sign | — |
| `authorization` | JWT (Bearer) token | — |
| `baseUrl` | SolidSign API base URL | `https://www.solidsign.com.br` |
| `cloudCredentials` | Cloud HSM/PSC credentials (JSON: hsmUrl, hsmToken, uuidCert) | — |
| `profile` | PBAD/ETSI signature profile | `ADRB` |
| `hashAlgorithm` | Hash algorithm | `SHA256` |
| `signaturePackaging` | CMS packaging | `ATTACHED` |
