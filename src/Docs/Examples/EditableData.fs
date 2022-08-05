module Examples.EditableData

open System
open Browser.Types
open Elmish
open Fable.Core.JsInterop
open Feliz
open Feliz.UseElmish
open Feliz.TanStack.Table
open Feliz.style

type Person = {
  Firstname: string
  Lastname: string
  Age: int
  Visits: int
  Status: string
  Progress: int
}

let makeData (count : int) =
    let statuses = [| "relationship"; "complicated"; "single" |]
    [| for c in 0..count do
           { Firstname = Faker.Name.FirstName()
             Lastname = Faker.Name.LastName()
             Age = Faker.DataType.Number(40)
             Visits = Faker.DataType.Number(1000)
             Progress = Faker.DataType.Number(100)
             Status = (Faker.Helpers.Shuffle statuses)[0] } |]

let defaultColumns : ColumnDefOptionProp<Person> list list = [
    [ columnDef.header "Name"
      columnDef.footer (fun props -> props.column.id)
      columnDef.columns [
          [ columnDef.accessorKey "Firstname"
            columnDef.cell (fun info -> info.getValue<_>())
            columnDef.footer (fun props -> props.column.id) ]
          [ columnDef.accessorFn (fun p -> p.Lastname)
            columnDef.id "Lastname"
            columnDef.cell (fun info -> info.getValue<_>())
            columnDef.header (fun _ -> Html.span [ prop.text "Last Name" ])
            columnDef.footer (fun props -> props.column.id) ]
      ] ]
    [ columnDef.header "Info"
      columnDef.footer (fun props -> props.column.id)
      columnDef.columns [
          [ columnDef.accessorKey "Age"
            columnDef.header (fun _ -> "Age")
            columnDef.footer (fun props -> props.column.id) ]
          [ columnDef.header "More Info"
            columnDef.columns [
                [ columnDef.accessorKey "Visits"
                  columnDef.header (fun _ -> Html.span [ prop.text "Visits" ])
                  columnDef.footer (fun props -> props.column.id) ]
                [ columnDef.accessorKey "Status"
                  columnDef.header "Status"
                  columnDef.footer (fun props -> props.column.id) ]
                [ columnDef.accessorKey "Progress"
                  columnDef.header "Profile Progress"
                  columnDef.footer (fun props -> props.column.id) ]
            ] ]
      ] ]
]

type State = {
    Table : Table<Person>
    ResizeMode : ColumnResizeMode
    ResizingHandler : (Event -> Table<Person>) option
}

type Msg =
    | ResizeModeChange of ColumnResizeMode
    | BeginResize of Event * Header<Person>
    | Resize of Event * (Event -> Table<Person>)
    | EndResize
    
let private rand = Random()    

let init () =
    let tableProps = [
        tableProps.data (makeData 100)
        tableProps.columns defaultColumns
        tableProps.columnResizeMode OnChange
        tableProps.enableColumnResizing true ]
    
    let table = Table.init<Person> tableProps
    
    { Table = table
      ResizeMode = OnChange
      ResizingHandler = None }, Cmd.none

let update (msg: Msg) (state: State) =
    match msg with
    | ResizeModeChange resizeMode ->
        let table = Table.setColumnSizingMode resizeMode state.Table
        { state with
            ResizeMode = resizeMode
            Table = table }, Cmd.none
        
    | BeginResize (event, header) ->
        let handler = Header.resizeHandler event header state.Table
        { state with
            ResizingHandler = Some handler
            Table = state.Table }, Cmd.none
        
    | Resize (event, handler) ->
        let table = handler event
        { state with
            Table = table }, Cmd.none
        
    | EndResize ->
        match state.ResizingHandler with
        | Some _ ->
            let table = Header.endResize state.Table
            { state with
                ResizingHandler = None
                Table = table }, Cmd.none
        | None -> state, Cmd.none
        
let view (state: State) (dispatch: Msg -> unit) =
    let beginResize h e = BeginResize (e, h) |> dispatch
    let move e = 
        match state.ResizingHandler with
         | Some p -> Resize (e, p) |> dispatch
         | None -> ()
    let endResize _ = EndResize  |> dispatch
         
    let table = 
        let thead =
            Html.thead [
                for headerGroup in Table.getHeaderGroups state.Table do
                     Html.tr [
                         prop.key headerGroup.Id
                         prop.style [
                             style.height (length.px 30)
                             width.fitContent
                         ]
                         prop.children [
                             for header in headerGroup.Headers do
                                 Html.th [
                                     prop.onMouseMove move
                                     prop.onTouchMove move
                                     prop.key header.Id
                                     prop.colSpan header.ColSpan
                                     prop.style [
                                         style.width (Header.getSize header)
                                         position.relative
                                     ]
                                     prop.children [
                                         Html.flexRender (
                                             header.IsPlaceholder,
                                             header.Column.ColumnDef.Header,
                                             Table.getContext header)
                                         Html.div [
                                             prop.onMouseDown (beginResize header)
                                             prop.onTouchStart (beginResize header)
                                             prop.style [
                                                 if Column.getIsResizing header.Column && state.ResizeMode = OnEnd then
                                                     transform.translateX (Table.getDeltaOffset state.Table |> length.px )
                                             ]
                                             prop.className [
                                                 "resizer"
                                                 if Column.getIsResizing header.Column then "isResizing"
                                             ]
                                         ]
                                     ]
                                 ]
                         ]
                     ]
            ]
            
        let tbody =
            Html.tbody [
                for row in (Table.getRowModel state.Table).Rows do
                    Html.tr [
                        prop.key row.Id
                        prop.children [
                            for cell in Table.getVisibleCells row do
                                Html.td [
                                    Html.flexRender(
                                        cell.Column.ColumnDef.Cell,
                                        Table.getContext cell)
                                ]
                        ]
                    ]
            ]
            
        Html.div [
            prop.className [ Bulma.P2 ]
            prop.children [
                Html.table [
                    prop.style [ style.width (Table.getCenterTotalSize state.Table) ]
                    prop.children [
                        thead
                        tbody
                    ]
                ]
            ]
        ]

    Html.div [
        prop.onMouseUp endResize
        prop.onMouseLeave endResize
        prop.onTouchEnd endResize
        prop.children [
            Html.p [
                prop.className [ Bulma.HasBackgroundWarning; Bulma.M2; Bulma.P2 ]
                prop.text "Work in progress..."
            ]
            Html.div [
                prop.className [ Bulma.Field ]
                prop.children [
                    Html.select [
                        prop.className [ Bulma.Select ]
                        prop.onChange (fun (e : Event) ->
                            match e.target?value, state.ResizeMode with
                            | "onChange", OnEnd -> ResizeModeChange OnChange |> dispatch
                            | "onEnd", OnChange -> ResizeModeChange OnEnd |> dispatch
                            | _ -> ())
                        prop.value (ColumnResizeMode.toString state.ResizeMode)
                        prop.children [
                            Html.option [
                                prop.text "onChange"
                            ]
                            Html.option [
                                prop.text "onEnd"
                            ]
                        ]
                    ]
                ]
            ]
            
            table
        ]
    ]
    
[<ReactComponent>]
let Component () =
    let state, dispatch = React.useElmish (init, update)
    view state dispatch