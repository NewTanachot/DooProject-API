========== [ How to use ] ==========

Just run DooProject application file and test API


================ [ Register API ] ================

url : https://localhost:7260/api/Auth/Register
method : POST
headers: { 'Content-Type': 'application/json' },
body: {
  "email": "user@example.com",
  "password": "string",
  "firstName": "string",
  "lastName": "string"
}


================ [ Login API ] ================

url : https://localhost:7260/api/Auth/Login
method : POST
headers: { 'Content-Type': 'application/json' },
body: {
  "email": "user@example.com",
  "password": "string"
}


================ [ GetProduct API ] ================

url : https://localhost:7260/api/Product/GetProduct
Method : GET
headers: { 
'Content-Type': 'application/json' 
'Authorization': 'Bearer {Your JWT}'
}












