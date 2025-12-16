import { useEffect, useRef, useState, useMemo } from 'react';
import { Network, Options } from 'vis-network';
import { DataSet } from 'vis-data';
import { useGetRecommendationsQuery, useGetUserSkillsQuery } from '../store/api/apiSlice';
import { useAppSelector } from '../store/hooks';
import { UserMatchDto } from '../types';
import { Loader2, ZoomIn, ZoomOut, Maximize2 } from 'lucide-react';

interface SkillNetworkGraphProps {
  onUserClick?: (user: UserMatchDto) => void;
}

// vis-network node/edge data types
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type VisNode = any;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type VisEdge = any;

// Dark theme colors
const COLORS = {
  background: '#0f172a', // slate-900
  currentUser: '#ec4899', // pink-500 (distinct from green)
  currentUserGlow: 'rgba(236, 72, 153, 0.5)',
  highMatch: '#22c55e', // green-500
  highMatchGlow: 'rgba(34, 197, 94, 0.4)',
  mediumMatch: '#f59e0b', // amber-500
  mediumMatchGlow: 'rgba(245, 158, 11, 0.4)',
  lowMatch: '#a78bfa', // violet-400 (distinct from text)
  lowMatchGlow: 'rgba(167, 139, 250, 0.3)',
  text: '#e2e8f0', // slate-200
  textMuted: '#94a3b8', // slate-400
  edge: '#475569', // slate-600
};

export const SkillNetworkGraph: React.FC<SkillNetworkGraphProps> = ({ onUserClick }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const networkRef = useRef<Network | null>(null);
  const nodesRef = useRef<DataSet<VisNode> | null>(null);
  const edgesRef = useRef<DataSet<VisEdge> | null>(null);
  const { user: currentUser } = useAppSelector((state) => state.auth);
  const { data: recommendations = [], isLoading: isLoadingRecs } = useGetRecommendationsQuery(20);
  const { data: userSkills = [] } = useGetUserSkillsQuery();
  const [selectedNode, setSelectedNode] = useState<UserMatchDto | null>(null);
  const [activeFilter, setActiveFilter] = useState<'all' | 'you' | 'high' | 'medium' | 'low'>('all');

  // Get skills the current user wants to learn (not offering)
  // Memoize to prevent useEffect from re-running on every render
  const wantedSkills = useMemo(() =>
    userSkills
      .filter(s => !s.isOffering)
      .map(s => s.skill?.name?.toLowerCase() || ''),
    [userSkills]
  );

  useEffect(() => {
    if (!containerRef.current || isLoadingRecs || !currentUser) return;

    // Create nodes with dark theme styling
    const nodes = new DataSet<VisNode>([
      // Current user node (center) with glow effect
      {
        id: currentUser.id,
        label: 'You',
        color: {
          background: COLORS.currentUser,
          border: COLORS.currentUser,
          highlight: { background: '#2dd4bf', border: '#5eead4' }, // teal-400, teal-300
          hover: { background: '#2dd4bf', border: '#5eead4' },
        },
        shape: 'circle',
        size: 40,
        font: { color: '#ffffff', size: 14, bold: true },
        shadow: {
          enabled: true,
          color: COLORS.currentUserGlow,
          size: 20,
          x: 0,
          y: 0,
        },
      },
      // Other users
      ...recommendations.map((rec) => {
        const nodeStyle = getNodeStyle(rec.compatibilityScore);
        return {
          id: rec.id,
          label: rec.name.split(' ')[0],
          color: {
            background: nodeStyle.color,
            border: nodeStyle.color,
            highlight: { background: nodeStyle.highlight, border: nodeStyle.highlight },
            hover: { background: nodeStyle.highlight, border: nodeStyle.highlight },
          },
          shape: 'circle',
          size: 22 + Math.min(rec.compatibilityScore * 0.12, 18),
          font: { color: COLORS.text, size: 11 },
          shadow: {
            enabled: true,
            color: nodeStyle.glow,
            size: 15,
            x: 0,
            y: 0,
          },
          title: `${rec.name}\nSkills: ${rec.skillsOffered.map(s => s.skillName).join(', ')}\nMatch: ${rec.compatibilityScore}%`,
        };
      }),
    ]);
    nodesRef.current = nodes;

    // Create edges with better styling
    const edges = new DataSet<VisEdge>();

    recommendations.forEach((rec) => {
      const matchingSkills = rec.skillsOffered.filter(offered =>
        wantedSkills.some(wanted =>
          offered.skillName.toLowerCase().includes(wanted) ||
          wanted.includes(offered.skillName.toLowerCase())
        )
      );

      if (matchingSkills.length > 0 || rec.compatibilityScore > 50) {
        const edgeStyle = getEdgeStyle(rec.compatibilityScore);
        edges.add({
          from: currentUser.id,
          to: rec.id,
          color: {
            color: edgeStyle.color,
            highlight: edgeStyle.highlight,
            hover: edgeStyle.highlight,
            opacity: 0.8,
          },
          width: Math.max(1.5, rec.compatibilityScore / 25),
          arrows: {
            to: { enabled: true, scaleFactor: 0.5, type: 'arrow' },
          },
          smooth: {
            enabled: true,
            type: 'curvedCW',
            roundness: 0.2,
          },
          title: matchingSkills.length > 0
            ? `Can teach: ${matchingSkills.map(s => s.skillName).join(', ')}`
            : `Match score: ${rec.compatibilityScore}%`,
        });
      }
    });
    edgesRef.current = edges;

    // Network options with improved physics
    const options: Options = {
      layout: {
        improvedLayout: false, // Disable layout recalculations after initial render
        hierarchical: false,
      },
      nodes: {
        borderWidth: 2,
        borderWidthSelected: 3,
        font: {
          face: 'Inter, system-ui, sans-serif',
        },
      },
      edges: {
        smooth: {
          enabled: true,
          type: 'curvedCW',
          roundness: 0.2,
        },
        arrowStrikethrough: false, // Prevent edge from drawing behind/through nodes
      },
      physics: {
        enabled: true,
        solver: 'forceAtlas2Based',
        forceAtlas2Based: {
          gravitationalConstant: -80,
          centralGravity: 0.01,
          springLength: 120,
          springConstant: 0.08,
          damping: 0.4,
          avoidOverlap: 0.5,
        },
        stabilization: {
          enabled: true,
          iterations: 200,
          updateInterval: 25,
          fit: false, // Don't auto-fit after stabilization
        },
        maxVelocity: 50,
        minVelocity: 0.1,
      },
      interaction: {
        hover: true,
        hoverConnectedEdges: true,
        tooltipDelay: 150,
        zoomView: true,
        dragView: true,
        dragNodes: true,
        selectable: false, // Disable built-in selection to prevent repositioning
        selectConnectedEdges: false,
      },
    };

    // Create network
    const network = new Network(containerRef.current, { nodes, edges }, options);
    networkRef.current = network;

    // Disable physics after initial stabilization to prevent jarring repositioning
    network.once('stabilized', () => {
      network.fit(); // Fit once to center the view
      network.setOptions({ physics: false });
      network.stopSimulation();
    });

    // Ensure physics stays disabled after dragging nodes
    network.on('dragEnd', () => {
      network.setOptions({ physics: false });
    });

    // Helper to get node position and preserve it during updates
    const getNodePosition = (nodeId: string | number) => {
      const positions = network.getPositions([nodeId]);
      return positions[nodeId] || { x: 0, y: 0 };
    };

    // Hover highlighting - dim non-connected nodes and edges
    network.on('hoverNode', (params) => {
      const hoveredNodeId = params.node;
      const connectedNodes = network.getConnectedNodes(hoveredNodeId) as (string | number)[];
      const connectedEdges = network.getConnectedEdges(hoveredNodeId) as (string | number)[];
      connectedNodes.push(hoveredNodeId);

      // Dim non-connected nodes (preserve positions)
      nodes.forEach((node: VisNode) => {
        if (!connectedNodes.includes(node.id)) {
          const pos = getNodePosition(node.id);
          nodes.update({ id: node.id, opacity: 0.15, x: pos.x, y: pos.y });
        }
      });

      // Dim edges not connected to hovered node
      edges.forEach((edge: VisEdge) => {
        if (!connectedEdges.includes(edge.id)) {
          edges.update({
            id: edge.id,
            color: { ...edge.color, opacity: 0.25 },
            arrows: { to: { enabled: false } }, // Hide arrows on dimmed edges
          });
        }
      });
    });

    network.on('blurNode', () => {
      // Restore all nodes (preserve positions)
      nodes.forEach((node: VisNode) => {
        const pos = getNodePosition(node.id);
        nodes.update({ id: node.id, opacity: 1, x: pos.x, y: pos.y });
      });
      // Restore all edges
      edges.forEach((edge: VisEdge) => {
        edges.update({
          id: edge.id,
          color: { ...edge.color, opacity: 0.8 },
          arrows: { to: { enabled: true, scaleFactor: 0.5, type: 'arrow' } },
        });
      });
    });

    // Handle node clicks
    network.on('click', (params) => {
      if (params.nodes.length > 0) {
        const nodeId = params.nodes[0];
        if (nodeId !== currentUser.id) {
          const clickedUser = recommendations.find(r => r.id === nodeId);
          if (clickedUser) {
            setSelectedNode(clickedUser);
          }
        }
      } else {
        setSelectedNode(null);
      }
    });

    // Handle double-click to open exchange modal
    network.on('doubleClick', (params) => {
      if (params.nodes.length > 0) {
        const nodeId = params.nodes[0];
        if (nodeId !== currentUser.id) {
          const clickedUser = recommendations.find(r => r.id === nodeId);
          if (clickedUser && onUserClick) {
            onUserClick(clickedUser);
          }
        }
      }
    });

    return () => {
      network.destroy();
    };
  }, [recommendations, isLoadingRecs, currentUser, wantedSkills, onUserClick]);

  const handleZoomIn = () => {
    if (networkRef.current) {
      const scale = networkRef.current.getScale();
      networkRef.current.moveTo({ scale: scale * 1.3 });
    }
  };

  const handleZoomOut = () => {
    if (networkRef.current) {
      const scale = networkRef.current.getScale();
      networkRef.current.moveTo({ scale: scale / 1.3 });
    }
  };

  const handleFit = () => {
    if (networkRef.current) {
      networkRef.current.fit({ animation: true });
    }
  };

  // Get match category for a user
  const getMatchCategory = (score: number): 'high' | 'medium' | 'low' => {
    if (score >= 70) return 'high';
    if (score >= 40) return 'medium';
    return 'low';
  };

  // Helper to get node position and preserve it during updates
  const getNodePosition = (nodeId: string | number) => {
    if (!networkRef.current) return { x: 0, y: 0 };
    const positions = networkRef.current.getPositions([nodeId]);
    return positions[nodeId] || { x: 0, y: 0 };
  };

  // Apply filter to highlight/dim nodes
  const applyFilter = (filter: 'all' | 'you' | 'high' | 'medium' | 'low') => {
    setActiveFilter(filter);

    if (!nodesRef.current || !edgesRef.current || !currentUser || !networkRef.current) return;

    const nodes = nodesRef.current;
    const edges = edgesRef.current;

    if (filter === 'all') {
      // Restore all nodes and edges (preserve positions)
      nodes.forEach((node: VisNode) => {
        const pos = getNodePosition(node.id);
        nodes.update({ id: node.id, opacity: 1, x: pos.x, y: pos.y });
      });
      edges.forEach((edge: VisEdge) => {
        edges.update({
          id: edge.id,
          color: { ...edge.color, opacity: 0.8 },
          arrows: { to: { enabled: true, scaleFactor: 0.5, type: 'arrow' } },
        });
      });
      return;
    }

    // Build list of matching node IDs
    const matchingNodeIds: (number | string)[] = [];

    if (filter === 'you') {
      matchingNodeIds.push(currentUser.id);
    } else {
      // Always include current user when filtering by match type
      matchingNodeIds.push(currentUser.id);
      recommendations.forEach((rec) => {
        if (getMatchCategory(rec.compatibilityScore) === filter) {
          matchingNodeIds.push(rec.id);
        }
      });
    }

    // Update node visibility (preserve positions)
    nodes.forEach((node: VisNode) => {
      const isMatching = matchingNodeIds.includes(node.id);
      const pos = getNodePosition(node.id);
      nodes.update({ id: node.id, opacity: isMatching ? 1 : 0.15, x: pos.x, y: pos.y });
    });

    // Update edge visibility - show edges connected to matching nodes
    edges.forEach((edge: VisEdge) => {
      const isConnected = matchingNodeIds.includes(edge.from) && matchingNodeIds.includes(edge.to);
      edges.update({
        id: edge.id,
        color: { ...edge.color, opacity: isConnected ? 0.8 : 0.1 },
        arrows: { to: { enabled: isConnected, scaleFactor: 0.5, type: 'arrow' } },
      });
    });
  };

  if (isLoadingRecs) {
    return (
      <div className="flex items-center justify-center py-12 bg-white rounded-lg border border-gray-200">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
        <span className="ml-2 text-gray-600">Building skill network...</span>
      </div>
    );
  }

  if (recommendations.length === 0) {
    return (
      <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
        <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg className="w-8 h-8 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
          </svg>
        </div>
        <p className="text-gray-600 mb-2">No connections to visualize yet</p>
        <p className="text-sm text-gray-500">
          Add skills to your profile to see potential matches in the network.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Filter Buttons */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <span className="text-gray-500 text-xs mr-1">Filter:</span>
          <button
            onClick={() => applyFilter('all')}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-all ${
              activeFilter === 'all'
                ? 'bg-gray-800 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            All
          </button>
          <button
            onClick={() => applyFilter('you')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium transition-all ${
              activeFilter === 'you'
                ? 'bg-pink-500 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-pink-100'
            }`}
          >
            <div className="w-2.5 h-2.5 rounded-full bg-pink-500"></div>
            You
          </button>
          <button
            onClick={() => applyFilter('high')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium transition-all ${
              activeFilter === 'high'
                ? 'bg-green-500 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-green-100'
            }`}
          >
            <div className="w-2.5 h-2.5 rounded-full bg-green-500"></div>
            High (70%+)
          </button>
          <button
            onClick={() => applyFilter('medium')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium transition-all ${
              activeFilter === 'medium'
                ? 'bg-amber-500 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-amber-100'
            }`}
          >
            <div className="w-2.5 h-2.5 rounded-full bg-amber-500"></div>
            Medium (40-70%)
          </button>
          <button
            onClick={() => applyFilter('low')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium transition-all ${
              activeFilter === 'low'
                ? 'bg-violet-500 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-violet-100'
            }`}
          >
            <div className="w-2.5 h-2.5 rounded-full bg-violet-400"></div>
            Low (&lt;40%)
          </button>
        </div>
        <p className="text-xs text-gray-500 mt-2">
          Click to filter by match level. Double-click a user to request a session.
        </p>
      </div>

      {/* Network Graph */}
      <div className="relative">
        <div
          ref={containerRef}
          className="skill-network-container"
        />

        {/* Zoom Controls */}
        <div className="absolute top-4 right-4 flex flex-col gap-2">
          <button
            onClick={handleZoomIn}
            className="p-2 bg-white rounded-lg shadow-lg hover:bg-gray-50 transition-colors border border-gray-200"
            title="Zoom in"
          >
            <ZoomIn className="w-4 h-4 text-gray-600" />
          </button>
          <button
            onClick={handleZoomOut}
            className="p-2 bg-white rounded-lg shadow-lg hover:bg-gray-50 transition-colors border border-gray-200"
            title="Zoom out"
          >
            <ZoomOut className="w-4 h-4 text-gray-600" />
          </button>
          <button
            onClick={handleFit}
            className="p-2 bg-white rounded-lg shadow-lg hover:bg-gray-50 transition-colors border border-gray-200"
            title="Fit to view"
          >
            <Maximize2 className="w-4 h-4 text-gray-600" />
          </button>
        </div>
      </div>

      {/* Selected User Info */}
      {selectedNode && (
        <div className="bg-white rounded-lg border border-gray-200 p-4">
          <div className="flex items-start justify-between">
            <div>
              <h3 className="font-medium text-gray-900">{selectedNode.name}</h3>
              <p className="text-sm text-gray-500 mt-1">
                Match Score: <span className="font-medium text-blue-600">{selectedNode.compatibilityScore}%</span>
              </p>
              <div className="mt-2">
                <p className="text-sm text-gray-600">Skills offered:</p>
                <div className="flex flex-wrap gap-1 mt-1">
                  {selectedNode.skillsOffered.map((skill) => (
                    <span
                      key={skill.skillId}
                      className="px-2 py-0.5 bg-gray-100 text-gray-700 text-xs rounded-full"
                    >
                      {skill.skillName}
                    </span>
                  ))}
                </div>
              </div>
            </div>
            <button
              onClick={() => onUserClick?.(selectedNode)}
              className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
            >
              Request Session
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

// Helper functions
function getNodeStyle(score: number): { color: string; highlight: string; glow: string } {
  if (score >= 70) {
    return {
      color: COLORS.highMatch,
      highlight: '#4ade80',
      glow: COLORS.highMatchGlow,
    };
  }
  if (score >= 40) {
    return {
      color: COLORS.mediumMatch,
      highlight: '#fbbf24',
      glow: COLORS.mediumMatchGlow,
    };
  }
  return {
    color: COLORS.lowMatch,
    highlight: '#c4b5fd', // violet-300
    glow: COLORS.lowMatchGlow,
  };
}

function getEdgeStyle(score: number): { color: string; highlight: string } {
  if (score >= 70) {
    return { color: COLORS.highMatch, highlight: '#4ade80' };
  }
  if (score >= 40) {
    return { color: COLORS.mediumMatch, highlight: '#fbbf24' };
  }
  return { color: COLORS.edge, highlight: '#64748b' };
}
