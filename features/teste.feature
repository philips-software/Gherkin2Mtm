# language: pt
Funcionalidade: Test
	This is a Gherkin2Mtm testing feature

@manual
@tc:1134468
@story:1220382
@story:1240179
Esquema do Cenário: [Teste com tabela em Português] Criação de um teste com tabela
	Dado que eu possuo um item <item>
	E o item tem a quantidade <quantidade>

	Quando for enviada uma mensagem de verificação do preço <preço>

	Então o subtotal será <subtotal>

Exemplos: 
	| item     | quantidade | preço | subtotal |
	| 00000001 | 1          | 2.50  | 2.50     |
	| 00000002 | 2          | 2     | 4        |
	| 00000003 | 3          | 2     | 6        |