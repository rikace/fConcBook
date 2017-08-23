[<AutoOpen>]
module Show

// ----------------------------
// Windows Forms


open System.Windows.Forms
open System.Drawing

type row = { ObjectView : string }
let form = new Form(Visible = true, Text = "F# Show", 
                    TopMost = true, Size = Size(600,600))

let textBox = 
    new RichTextBox(Dock = DockStyle.Fill, Text = "Data First Sample",  
                    Font = new Font("Lucida Console",16.0f,FontStyle.Bold),
                    ForeColor = Color.DarkBlue)

let image = 
    new PictureBox(Dock = DockStyle.Fill,  
                    Font = new Font("Lucida Console",16.0f,FontStyle.Bold),
                    ForeColor = Color.DarkBlue)

let dataGrid = new DataGridView(Dock = DockStyle.Fill,
                                Text = "Data View",
                                Font = new Font("Lucida Console",16.0f,FontStyle.Bold),
                                ForeColor = Color.DarkBlue)

form.Controls.Add(textBox)


let showI x = 
    if not (form.Controls.Contains(image)) then
        form.Controls.Clear()
        form.Controls.Add(image)
    image.Image <- x
    System.Windows.Forms.Application.DoEvents()
    x


let showT x = 
    if not (form.Controls.Contains(textBox)) then
        form.Controls.Clear()
        form.Controls.Add(textBox)
    textBox.Text <- sprintf "%40A" x
    System.Windows.Forms.Application.DoEvents()
    x

let showG (x: 'a when 'a :> System.Collections.IEnumerable) = 
    if not (form.Controls.Contains(dataGrid)) then
        form.Controls.Clear()
        form.Controls.Add(dataGrid)    
    dataGrid.Columns.[0].Width <- 400
    dataGrid.DataSource <- x
    System.Windows.Forms.Application.DoEvents()
    x
(*
[<AutoOpen>]
module Show
open System.Windows.Forms
open System.Drawing
let form = 
    new Form(Visible = true, 
             Text = "A Simple F# Form", 
             TopMost = true, 
             Size = Size(600,600))
let textBox = 
    new RichTextBox(Dock = DockStyle.Fill, 
                    Text = "F# - Write Simple Code to Solve Complex Problems",
                    Font = new Font("Lucida Console",16.0f,FontStyle.Bold),
                    ForeColor = Color.DarkBlue)
form.Controls.Add(textBox)
let show x = 
   textBox.Text <- sprintf "%40A" x
   Application.DoEvents()
*)        