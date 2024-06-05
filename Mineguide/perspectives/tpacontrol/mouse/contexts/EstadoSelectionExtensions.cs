using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements;
using static Mineguide.perspectives.tpacontrol.mouse.contexts.EstadoSelectionExtensions;

namespace Mineguide.perspectives.tpacontrol.mouse.contexts
{
    public static class EstadoSelectionExtensions
    {
        public static System.Windows.Media.Brush SelectionColor = System.Windows.Media.Brushes.Green;
        public static System.Windows.Media.Brush ParallelismBlockColor = System.Windows.Media.Brushes.White;
        public static System.Windows.Media.Brush BlockBackgroundColor = System.Windows.Media.Brushes.White;
        public static System.Windows.Media.Brush BlockTextColor = System.Windows.Media.Brushes.LightGray;
        //public static System.Windows.Media.Brush BlockSelectionBorderColor = System.Windows.Media.Brushes.LightGray;

        #region SelectionType and management
        public enum SelectionType
        {
            None = 0x0,
            Selected = 0x1,            
            Blocked = 0x2,
            Highlighted = 0x4,
            ParalelismBlocked = 0x8, // to block some actions after parallelism           
        }

        public static void SetSelectionType(Estado state, SelectionType type, bool activate)
        {
            if (state == null || state.NodeTextBlock == null) return;


            int result = (int)SelectionType.None;
            object? original = state.NodeTextBlock.Tag;
            if (original == null)
            {
                result = (int)type;
            }
            else if (original is int i)
            {
                if (activate)
                {
                    result = i | (int)type; // activar el bit
                }
                else
                {
                    result = i & ~(int)type; // desactivar el bit
                }
            }
            else
            {
                throw new ArgumentException("EstadoSelectionExtensions error: Nodo.Tag must be an int");
            }
            state.NodeTextBlock.Tag = result;
        }

        public static bool IsSelectionType(Estado state, SelectionType type)
        {
            object? original = state?.NodeTextBlock?.Tag;
            if (original == null)
            {
                return false;
            }
            else if (original is int i)
            {
                return (i & (int)type) == (int)type;
            }
            throw new ArgumentException("EstadoSelectionExtensions error: Nodo.NodeTextBlock.Tag must be an int");
        }

        public static void DoSelectionType(this Estado state, SelectionType selectionType, System.Windows.Media.Brush brush)
        {
            if (!IsBlocked(state))
            {
                // El estado inicial y final no se pueden seleccionar pq tiene el color fijo a fuego
                if (state is EstadoInicial) return;
                if (state is EstadoFinal) return;

                state.NodeColor = brush;
                SetSelectionType(state, selectionType, true);
            }
        }

        public static void UndoSelectionType(this Estado state, SelectionType selectionType, System.Windows.Media.Brush brush)
        {
            if (!IsBlocked(state) || selectionType == SelectionType.Blocked) // si esta bloqueado no se puede cambiar salvo para desbloquearlo
            {
                if (IsSelectionType(state, selectionType))
                {
                    state.NodeColor = brush;
                    SetSelectionType(state, selectionType, false);
                }
            }
        }

        public static void UndoSelectionType(this Estado state, SelectionType selectionType)
        {
            System.Windows.Media.Brush brush = state.NodeColorOriginal;
            UndoSelectionType(state, selectionType, brush);
        }

        #endregion SelectionType and management        

        public static void Select(this Estado state)
        {
            //if (!IsBlocked(state))
            //{
            state.DoSelectionType(SelectionType.Selected, SelectionColor);
            //}
        }

        public static void Deselect(this Estado state)
        {
            if (IsSelected(state))
            {
                // cambio colores solo si no esta bloqueado, lo que si cambio siempre es el estado de seleccion
                //if (!IsBlocked(state)) 
                //{
                if (state.IsParallelismBlocked())
                {
                    state.NodeColor = ParallelismBlockColor;
                }
                else
                {
                    state.NodeColor = state.NodeColorOriginal;
                }
                //}
                SetSelectionType(state, SelectionType.Selected, false); // para saber si esta seleccionado o no                
            }
        }

        public static bool IsSelected(this Estado state) => IsSelectionType(state, SelectionType.Selected);

        public static void Block(this Estado state)
        {
            state.TextColor = BlockTextColor;
            //state.BorderColor = System.Windows.Media.Brushes.LightGray;
            state.DoSelectionType(SelectionType.Blocked, BlockBackgroundColor);
        }

        public static void Unblock(this Estado state)
        {            
            state.UndoSelectionType(SelectionType.Blocked); // color original
        }

        public static bool IsBlocked(this Estado state) => IsSelectionType(state, SelectionType.Blocked);

        public static void ParallelismBlock(this Estado state)
        {
            state.DoSelectionType(SelectionType.ParalelismBlocked, ParallelismBlockColor);
        }

        public static void ParallelismUnblock(this Estado state)
        {
            state.UndoSelectionType(SelectionType.ParalelismBlocked); // color original
        }

        public static bool IsParallelismBlocked(this Estado state) => IsSelectionType(state, SelectionType.ParalelismBlocked);

        public static void Highlight(this Estado state, System.Windows.Media.Brush brush)
        {
            state.DoSelectionType(SelectionType.Highlighted, brush);            
        }

        public static void UndoHighlight(this Estado state, System.Windows.Media.Brush brush)
        {
            state.UndoSelectionType(SelectionType.Highlighted, brush);
        }

        public static void UndoHighlight(this Estado state)
        {
            if (state.IsParallelismBlocked())
            {
                UndoHighlight(state, ParallelismBlockColor);
            }
            if (state.IsSelected())
            {
                UndoHighlight(state, SelectionColor);
            }
            else
            {
                UndoHighlight(state, state.NodeColorOriginal);
            }

        }
        public static bool IsHighlighted(this Estado state) => IsSelectionType(state, SelectionType.Highlighted);





       
        

    }
}
